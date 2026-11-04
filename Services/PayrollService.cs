using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Enums;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Models.Entities;
using Microsoft.Extensions.Options;
using ERP_HRM_CMS.Services.Payroll;

namespace ERP_HRM_CMS.Services
{
    public class PayrollService : IPayrollService
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IOptions<PayrollOptions> _options;

        public PayrollService(AppDbContext context, INotificationService notificationService, IOptions<PayrollOptions> options)
        {
            _context = context;
            _notificationService = notificationService;
            _options = options;
        }

        public async Task<PayrollPeriod> CalculatePayrollAsync(int month, int year, int standardDays, int? creatorUserId = null)
        {
            var period = await _context.PayrollPeriods
                .Include(p => p.SalarySlips)
                .FirstOrDefaultAsync(p => p.Month == month && p.Year == year);

            if (period == null)
            {
                period = new PayrollPeriod
                {
                    Name = $"Bảng lương Kỳ Tháng {month:D2}/{year}",
                    Month = month,
                    Year = year,
                    StandardWorkDays = standardDays,
                    Status = PayrollPeriodStatus.Calculated,
                    CreatedBy = creatorUserId,
                    CreatedAt = VietnamTime.Now
                };
                _context.PayrollPeriods.Add(period);
                await _context.SaveChangesAsync();
            }
            else
            {
                period.StandardWorkDays = standardDays;
                period.Status = PayrollPeriodStatus.Calculated;
            }

            int daysInMonth = DateTime.DaysInMonth(year, month);
            var startDate = new DateTime(year, month, 1);
            var endDate = new DateTime(year, month, daysInMonth);

            // Tham số của kỳ: phiên bản có hiệu lực tại ngày 01 của tháng. Thiếu cấu hình thì báo lỗi, không tự đoán.
            var rules = PayrollRuleResolver.Resolve(_options.Value, startDate);

            // 1. Nhân viên đang làm việc kèm chức vụ, hợp đồng, chi tiết ngân hàng
            var employees = await _context.Employees
                .Include(e => e.Position)
                .Include(e => e.EmployeeDetail)
                .Include(e => e.Contracts)
                .Where(e => e.Status == "Active")
                .ToListAsync();

            // 2. Chấm công trong tháng
            var attendances = await _context.AttendanceDailies
                .Where(a => a.WorkDate >= startDate && a.WorkDate <= endDate)
                .ToListAsync();

            // 3. Đơn nghỉ đã duyệt có ngày giao với tháng
            var approvedLeaves = await _context.LeaveRequests
                .Where(l => l.Status == LeaveRequestStatus.DirectorApproved &&
                           ((l.FromDate >= startDate && l.FromDate <= endDate) ||
                            (l.ToDate >= startDate && l.ToDate <= endDate)))
                .ToListAsync();

            // 4. Loại phụ cấp đang dùng: số tiền, cờ miễn thuế và hạn mức lấy từ bảng dữ liệu
            var allowanceTypes = await _context.PayrollAllowanceTypes
                .Where(t => t.IsActive)
                .OrderBy(t => t.Id)
                .ToListAsync();
            var allowanceLines = allowanceTypes
                .Select(t => new AllowanceLine(t.Code, t.DefaultAmount, t.IsTaxExempt, t.TaxExemptLimit))
                .ToList();

            // 5. Số người phụ thuộc còn hiệu lực trong tháng, theo nhân viên
            var dependentEmployeeIds = await _context.EmployeeDependents
                .Where(d => d.FromDate <= endDate && (d.ToDate == null || d.ToDate >= startDate))
                .Select(d => d.EmployeeId)
                .ToListAsync();
            var dependentCounts = dependentEmployeeIds
                .GroupBy(id => id)
                .ToDictionary(g => g.Key, g => g.Count());

            decimal totalGross = 0;
            decimal totalNet = 0;
            decimal totalInsurance = 0;
            decimal totalEmployerInsurance = 0;
            decimal totalPIT = 0;

            foreach (var emp in employees)
            {
                var activeContract = emp.Contracts
                    .Where(c => c.Status == "Active")
                    .OrderByDescending(c => c.StartDate)
                    .FirstOrDefault();

                decimal baseSalary = activeContract?.BasicSalary ?? (emp.Position?.BaseSalary > 0 ? emp.Position.BaseSalary : 10000000m);
                decimal? declaredInsurance = activeContract != null && activeContract.InsuranceSalary > 0
                    ? activeContract.InsuranceSalary
                    : null;

                // Thống kê chấm công
                var empAtts = attendances.Where(a => a.EmployeeId == emp.Id).ToList();
                decimal actualWorkDays = empAtts.Count(a => a.AttendanceStatus == "Present" || a.AttendanceStatus == "Late" || a.AttendanceStatus == "Early");
                int totalLateMinutes = empAtts.Sum(a => a.LateMinutes);
                decimal overtimeHours = empAtts.Sum(a => a.OvertimeHours);

                // Thống kê đơn từ
                var empLeaves = approvedLeaves.Where(l => l.EmployeeId == emp.Id).ToList();
                decimal paidLeaveDays = empLeaves
                    .Where(l => l.RequestType == "ANNUAL_LEAVE" || l.RequestType == "SICK_LEAVE" || l.RequestType == "BUSINESS_TRIP")
                    .Sum(l => l.TotalDays);
                decimal unpaidLeaveDays = empLeaves
                    .Where(l => l.RequestType == "UNPAID_LEAVE")
                    .Sum(l => l.TotalDays);

                // Phụ cấp chức vụ (chịu thuế) và thưởng
                decimal posAllowanceRate = emp.Position?.AllowanceRate ?? 0;
                decimal positionAllowance = PayrollCalculator.Money(baseSalary * posAllowanceRate);
                decimal bonus = emp.Position?.Code == "NV-KD" ? 2500000m : 1000000m; // TODO: đưa mức thưởng vào cấu hình

                int dependentCount = dependentCounts.GetValueOrDefault(emp.Id);

                var input = new PayrollInput
                {
                    BaseSalary = baseSalary,
                    DeclaredInsuranceSalary = declaredInsurance,
                    StandardDays = standardDays,
                    HasAttendance = empAtts.Count > 0,
                    ActualWorkDays = actualWorkDays,
                    PaidLeaveDays = paidLeaveDays,
                    UnpaidLeaveDays = unpaidLeaveDays,
                    TotalLateMinutes = totalLateMinutes,
                    OvertimeHours = overtimeHours,
                    PositionAllowance = positionAllowance,
                    Bonus = bonus,
                    DependentCount = dependentCount,
                    Allowances = allowanceLines
                };

                var result = PayrollCalculator.Calculate(input, rules);

                // Phụ cấp ăn trưa (mã LUNCH) và các phụ cấp khác hiển thị ở hai cột của phiếu lương
                decimal lunchAllowance = allowanceLines.Where(l => l.Code == "LUNCH").Sum(l => l.Amount);
                decimal otherAllowance = allowanceLines.Where(l => l.Code != "LUNCH").Sum(l => l.Amount);

                totalGross += result.Gross;
                totalNet += result.NetSalary;
                totalInsurance += result.EmployeeTotal;
                totalEmployerInsurance += result.EmployerTotal;
                totalPIT += result.PersonalIncomeTax;

                var slip = period.SalarySlips.FirstOrDefault(s => s.EmployeeId == emp.Id);
                if (slip == null)
                {
                    slip = new SalarySlip
                    {
                        PayrollPeriodId = period.Id,
                        EmployeeId = emp.Id
                    };
                    period.SalarySlips.Add(slip);
                }

                slip.BasicSalary = baseSalary;
                slip.InsuranceSalary = result.InsuranceBase;
                slip.StandardDays = standardDays;
                slip.ActualWorkDays = actualWorkDays;
                slip.PaidLeaveDays = paidLeaveDays;
                slip.UnpaidLeaveDays = unpaidLeaveDays;
                slip.OvertimeHours = overtimeHours;
                slip.StandardWorkSalary = result.StandardWorkSalary;
                slip.OvertimeSalary = result.OvertimeSalary;
                slip.PositionAllowance = positionAllowance;
                slip.LunchAllowance = lunchAllowance;
                slip.OtherAllowance = otherAllowance;
                slip.Bonus = bonus;
                slip.GrossSalary = result.Gross;
                slip.LatePenalty = result.LatePenalty;

                // Phần người lao động đóng (trừ vào lương)
                slip.SocialInsurance = result.EmployeeSocial;
                slip.HealthInsurance = result.EmployeeHealth;
                slip.UnemploymentInsurance = result.EmployeeUnemployment;
                slip.TotalInsurance = result.EmployeeTotal;

                // Phần người sử dụng lao động đóng (không trừ vào lương, dùng cho bảng kê)
                slip.EmployerSocialInsurance = result.EmployerSocial;
                slip.EmployerHealthInsurance = result.EmployerHealth;
                slip.EmployerUnemploymentInsurance = result.EmployerUnemployment;
                slip.EmployerAccidentInsurance = result.EmployerAccident;
                slip.TotalEmployerInsurance = result.EmployerTotal;

                // Thuế TNCN
                slip.DependentCount = dependentCount;
                slip.PersonalDeduction = result.PersonalDeduction;
                slip.DependentDeduction = result.DependentDeduction;
                slip.TaxExemptAllowance = result.TaxExemptAllowance;
                slip.TaxableIncome = result.TaxableIncome;
                slip.PersonalIncomeTax = result.PersonalIncomeTax;
                slip.NetSalary = result.NetSalary;

                slip.Status = SalarySlipStatus.Draft;
                slip.Note = BuildSlipNote(standardDays, rules);
            }

            period.TotalGrossSalary = totalGross;
            period.TotalNetSalary = totalNet;
            period.TotalInsurance = totalInsurance;
            period.TotalEmployerInsurance = totalEmployerInsurance;
            period.TotalPIT = totalPIT;

            await _context.SaveChangesAsync();
            return period;
        }

        /// <summary>Ghi chú phiếu lương: công chuẩn và cảnh báo nếu còn quy định chưa xác nhận.</summary>
        private static string BuildSlipNote(int standardDays, PayrollRules rules)
        {
            var note = $"Đã tính tự động theo công chuẩn {standardDays} ngày.";
            if (rules.PendingConfirmations.Count > 0)
            {
                note += $" Có {rules.PendingConfirmations.Count} quy định chưa xác nhận nguồn (TODO trong cấu hình Payroll).";
            }
            return note.Length <= 500 ? note : note[..500];
        }

        public async Task<bool> SendPayslipsAsync(int payrollPeriodId, int? employeeId = null)
        {
            var period = await _context.PayrollPeriods
                .Include(p => p.SalarySlips)
                    .ThenInclude(s => s.Employee)
                .FirstOrDefaultAsync(p => p.Id == payrollPeriodId);

            if (period == null) return false;

            var targetSlips = period.SalarySlips.AsQueryable();
            if (employeeId.HasValue && employeeId.Value > 0)
            {
                targetSlips = targetSlips.Where(s => s.EmployeeId == employeeId.Value);
            }

            var slipsList = targetSlips.ToList();
            var empIds = slipsList.Select(s => s.EmployeeId).ToList();

            // Tìm Users gắn với các Employees này
            var users = await _context.Users
                .Where(u => u.EmployeeId.HasValue && empIds.Contains(u.EmployeeId.Value))
                .ToListAsync();

            foreach (var slip in slipsList)
            {
                slip.Status = SalarySlipStatus.Sent;
                var linkedUser = users.FirstOrDefault(u => u.EmployeeId == slip.EmployeeId);

                string title = $"[PHIẾU LƯƠNG] {period.Name}";
                string content = $"Phiếu lương của bạn ({slip.Employee.FullName}): Thực lĩnh {slip.NetSalary:N0} VNĐ. Vui lòng kiểm tra và xác nhận.";
                string linkUrl = $"/BangLuong/PhieuLuong/{slip.Id}";

                if (linkedUser != null)
                {
                    await _notificationService.SendToUserAsync(linkedUser.Id, title, content, linkUrl, "PAYROLL");
                }
            }

            // Đồng thời gửi thông báo cho Ban Quản trị / HR
            await _notificationService.SendToRoleAsync("SUPERADMIN", 
                $"Đã phát hành phiếu lương {period.Name}", 
                $"Đã gửi phiếu lương thành công cho {slipsList.Count} nhân sự. Tổng quỹ lương thực lĩnh: {slipsList.Sum(s => s.NetSalary):N0} VNĐ.",
                $"/BangLuong/Index?month={period.Month}&year={period.Year}",
                "PAYROLL");

            await _context.SaveChangesAsync();
            return true;
        }

        public byte[] GeneratePayrollExcel(PayrollPeriod period)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add($"Luong_T{period.Month}_{period.Year}");

            // Tiêu đề & Thông tin doanh nghiệp
            ws.Cell(1, 1).Value = "TẬP ĐOÀN CÔNG NGHỆ TIÊN PHONG - ERP ENTERPRISE";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#1E293B");

            ws.Cell(2, 1).Value = $"BẢNG THANH TOÁN TIỀN LƯƠNG & CÁC KHOẢN TRÍCH THEO LƯƠNG - KỲ THÁNG {period.Month:D2}/{period.Year}";
            ws.Cell(2, 1).Style.Font.Bold = true;
            ws.Cell(2, 1).Style.Font.FontSize = 12;
            ws.Cell(2, 1).Style.Font.FontColor = XLColor.FromHtml("#2563EB");

            ws.Cell(3, 1).Value = $"Công chuẩn: {period.StandardWorkDays} ngày | Trạng thái: {WorkflowStatusCodes.ToCode(WorkflowStatusCodes.PayrollPeriod, period.Status)} | Xuất ngày: {VietnamTime.Now:dd/MM/yyyy HH:mm}";
            ws.Cell(3, 1).Style.Font.Italic = true;
            ws.Cell(3, 1).Style.Font.FontSize = 10;
            ws.Cell(3, 1).Style.Font.FontColor = XLColor.Gray;

            // Bảng Headers
            string[] headers = new[]
            {
                "STT", "Mã NV", "Họ và Tên", "Phòng Ban", "Chức Vụ", "Số TK Ngân Hàng",
                "Lương Hợp Đồng", "Công Chuẩn", "Công Thực", "Phép Hưởng Lương", "Giờ OT",
                "Lương Ngày Công", "Tiền OT (150%)", "Phụ Cấp Chức Vụ", "Phụ Cấp Ăn Trưa", "Phụ Cấp Khác",
                "Thưởng Hiệu Suất", "TỔNG THU NHẬP (GROSS)", "BHXH (8%)", "BHYT (1.5%)", "BHTN (1%)",
                "TỔNG BẢO HIỂM", "Phạt Đi Muộn", "Thuế TNCN", "THỰC LĨNH (NET)", "Trạng Thái", "Ký Nhận"
            };

            int startRow = 5;
            for (int col = 0; col < headers.Length; col++)
            {
                var cell = ws.Cell(startRow, col + 1);
                cell.Value = headers[col];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E293B");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }
            ws.Row(startRow).Height = 28;

            int rowIdx = startRow + 1;
            int stt = 1;
            foreach (var slip in period.SalarySlips.OrderBy(s => s.Employee.EmployeeCode))
            {
                ws.Cell(rowIdx, 1).Value = stt++;
                ws.Cell(rowIdx, 2).Value = slip.Employee.EmployeeCode;
                ws.Cell(rowIdx, 3).Value = slip.Employee.FullName;
                ws.Cell(rowIdx, 4).Value = slip.Employee.Department?.Name ?? "-";
                ws.Cell(rowIdx, 5).Value = slip.Employee.Position?.Name ?? "-";
                ws.Cell(rowIdx, 6).Value = $"{slip.Employee.EmployeeDetail?.BankAccountNumber} ({slip.Employee.EmployeeDetail?.BankName})";
                
                ws.Cell(rowIdx, 7).Value = slip.BasicSalary;
                ws.Cell(rowIdx, 8).Value = slip.StandardDays;
                ws.Cell(rowIdx, 9).Value = slip.ActualWorkDays;
                ws.Cell(rowIdx, 10).Value = slip.PaidLeaveDays;
                ws.Cell(rowIdx, 11).Value = slip.OvertimeHours;

                ws.Cell(rowIdx, 12).Value = slip.StandardWorkSalary;
                ws.Cell(rowIdx, 13).Value = slip.OvertimeSalary;
                ws.Cell(rowIdx, 14).Value = slip.PositionAllowance;
                ws.Cell(rowIdx, 15).Value = slip.LunchAllowance;
                ws.Cell(rowIdx, 16).Value = slip.OtherAllowance;
                ws.Cell(rowIdx, 17).Value = slip.Bonus;
                ws.Cell(rowIdx, 18).Value = slip.GrossSalary;

                ws.Cell(rowIdx, 19).Value = slip.SocialInsurance;
                ws.Cell(rowIdx, 20).Value = slip.HealthInsurance;
                ws.Cell(rowIdx, 21).Value = slip.UnemploymentInsurance;
                ws.Cell(rowIdx, 22).Value = slip.TotalInsurance;
                ws.Cell(rowIdx, 23).Value = slip.LatePenalty;
                ws.Cell(rowIdx, 24).Value = slip.PersonalIncomeTax;
                ws.Cell(rowIdx, 25).Value = slip.NetSalary;
                ws.Cell(rowIdx, 26).Value = WorkflowStatusCodes.ToCode(WorkflowStatusCodes.SalarySlip, slip.Status);
                ws.Cell(rowIdx, 27).Value = "";

                // Format tiền tệ
                int[] moneyCols = { 7, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25 };
                foreach (int col in moneyCols)
                {
                    ws.Cell(rowIdx, col).Style.NumberFormat.Format = "#,##0";
                }

                // Căn lề
                ws.Cell(rowIdx, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(rowIdx, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(rowIdx, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(rowIdx, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(rowIdx, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(rowIdx, 11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(rowIdx, 26).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Tô màu Thực lĩnh và Gross
                ws.Cell(rowIdx, 18).Style.Font.Bold = true;
                ws.Cell(rowIdx, 25).Style.Font.Bold = true;
                ws.Cell(rowIdx, 25).Style.Font.FontColor = XLColor.FromHtml("#16A34A");

                rowIdx++;
            }

            // Dòng Tổng Cộng
            ws.Cell(rowIdx, 3).Value = "TỔNG CỘNG";
            ws.Cell(rowIdx, 3).Style.Font.Bold = true;
            ws.Cell(rowIdx, 18).FormulaA1 = $"SUM(R{startRow + 1}:R{rowIdx - 1})";
            ws.Cell(rowIdx, 22).FormulaA1 = $"SUM(V{startRow + 1}:V{rowIdx - 1})";
            ws.Cell(rowIdx, 24).FormulaA1 = $"SUM(X{startRow + 1}:X{rowIdx - 1})";
            ws.Cell(rowIdx, 25).FormulaA1 = $"SUM(Y{startRow + 1}:Y{rowIdx - 1})";
            ws.Row(rowIdx).Style.Font.Bold = true;
            ws.Row(rowIdx).Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
            ws.Cell(rowIdx, 18).Style.NumberFormat.Format = "#,##0";
            ws.Cell(rowIdx, 22).Style.NumberFormat.Format = "#,##0";
            ws.Cell(rowIdx, 24).Style.NumberFormat.Format = "#,##0";
            ws.Cell(rowIdx, 25).Style.NumberFormat.Format = "#,##0";

            // Viền bảng
            var tableRange = ws.Range(startRow, 1, rowIdx, headers.Length);
            tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        /// <summary>
        /// Bảng kê trích nộp bảo hiểm phần người sử dụng lao động, kèm phần người lao động để đối chiếu.
        /// </summary>
        public byte[] GenerateEmployerInsuranceExcel(PayrollPeriod period)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add($"BHXH_T{period.Month}_{period.Year}");

            ws.Cell(1, 1).Value = "BẢNG KÊ TRÍCH NỘP BẢO HIỂM XÃ HỘI, Y TẾ, THẤT NGHIỆP, TNLĐ-BNN";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#1E293B");

            ws.Cell(2, 1).Value = $"Kỳ lương: tháng {period.Month:D2}/{period.Year} | Phần người sử dụng lao động đóng, kèm phần người lao động để đối chiếu";
            ws.Cell(2, 1).Style.Font.Italic = true;
            ws.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;

            ws.Cell(3, 1).Value = $"Xuất ngày: {VietnamTime.Now:dd/MM/yyyy HH:mm}";
            ws.Cell(3, 1).Style.Font.FontColor = XLColor.Gray;

            string[] headers =
            {
                "STT", "Mã NV", "Họ và Tên", "Phòng Ban", "Lương Đóng BH",
                "BHXH NSDLĐ (Hưu trí, tử tuất, ốm đau, thai sản)", "BHYT NSDLĐ", "BHTN NSDLĐ", "TNLĐ-BNN NSDLĐ",
                "Tổng NSDLĐ", "Tổng NLĐ", "Tổng trích nộp"
            };

            const int startRow = 5;
            for (int col = 0; col < headers.Length; col++)
            {
                var cell = ws.Cell(startRow, col + 1);
                cell.Value = headers[col];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E293B");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Alignment.WrapText = true;
            }
            ws.Row(startRow).Height = 42;

            int rowIdx = startRow + 1;
            int stt = 1;
            decimal sumBase = 0, sumSocial = 0, sumHealth = 0, sumUnemp = 0, sumAccident = 0, sumEr = 0, sumEe = 0, sumAll = 0;

            foreach (var slip in period.SalarySlips.OrderBy(s => s.Employee.EmployeeCode))
            {
                decimal totalEmployer = slip.TotalEmployerInsurance;
                decimal totalEmployee = slip.TotalInsurance;

                ws.Cell(rowIdx, 1).Value = stt++;
                ws.Cell(rowIdx, 2).Value = slip.Employee.EmployeeCode;
                ws.Cell(rowIdx, 3).Value = slip.Employee.FullName;
                ws.Cell(rowIdx, 4).Value = slip.Employee.Department?.Name ?? "-";
                ws.Cell(rowIdx, 5).Value = slip.InsuranceSalary;
                ws.Cell(rowIdx, 6).Value = slip.EmployerSocialInsurance;
                ws.Cell(rowIdx, 7).Value = slip.EmployerHealthInsurance;
                ws.Cell(rowIdx, 8).Value = slip.EmployerUnemploymentInsurance;
                ws.Cell(rowIdx, 9).Value = slip.EmployerAccidentInsurance;
                ws.Cell(rowIdx, 10).Value = totalEmployer;
                ws.Cell(rowIdx, 11).Value = totalEmployee;
                ws.Cell(rowIdx, 12).Value = (totalEmployer + totalEmployee);

                for (int col = 5; col <= 12; col++)
                {
                    ws.Cell(rowIdx, col).Style.NumberFormat.Format = "#,##0";
                }
                ws.Cell(rowIdx, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(rowIdx, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                sumBase += slip.InsuranceSalary;
                sumSocial += slip.EmployerSocialInsurance;
                sumHealth += slip.EmployerHealthInsurance;
                sumUnemp += slip.EmployerUnemploymentInsurance;
                sumAccident += slip.EmployerAccidentInsurance;
                sumEr += totalEmployer;
                sumEe += totalEmployee;
                sumAll += totalEmployer + totalEmployee;

                rowIdx++;
            }

            // Dòng tổng cộng: tính sẵn bằng số liệu từng dòng để không phụ thuộc công thức trong Excel
            ws.Cell(rowIdx, 3).Value = "TỔNG CỘNG";
            ws.Cell(rowIdx, 5).Value = sumBase;
            ws.Cell(rowIdx, 6).Value = sumSocial;
            ws.Cell(rowIdx, 7).Value = sumHealth;
            ws.Cell(rowIdx, 8).Value = sumUnemp;
            ws.Cell(rowIdx, 9).Value = sumAccident;
            ws.Cell(rowIdx, 10).Value = sumEr;
            ws.Cell(rowIdx, 11).Value = sumEe;
            ws.Cell(rowIdx, 12).Value = sumAll;
            ws.Row(rowIdx).Style.Font.Bold = true;
            ws.Row(rowIdx).Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
            for (int col = 5; col <= 12; col++)
            {
                ws.Cell(rowIdx, col).Style.NumberFormat.Format = "#,##0";
            }

            var tableRange = ws.Range(startRow, 1, rowIdx, headers.Length);
            tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
