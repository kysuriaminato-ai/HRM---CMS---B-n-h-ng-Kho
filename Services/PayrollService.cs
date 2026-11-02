using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Models.Entities;

namespace ERP_HRM_CMS.Services
{
    public class PayrollService : IPayrollService
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;

        public PayrollService(AppDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
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
                    Status = "CALCULATED",
                    CreatedBy = creatorUserId,
                    CreatedAt = DateTime.Now
                };
                _context.PayrollPeriods.Add(period);
                await _context.SaveChangesAsync();
            }
            else
            {
                period.StandardWorkDays = standardDays;
                period.Status = "CALCULATED";
            }

            int daysInMonth = DateTime.DaysInMonth(year, month);
            var startDate = new DateTime(year, month, 1);
            var endDate = new DateTime(year, month, daysInMonth);

            // 1. Lấy danh sách nhân viên đang Active kèm chức vụ, hợp đồng, chi tiết ngân hàng
            var employees = await _context.Employees
                .Include(e => e.Position)
                .Include(e => e.EmployeeDetail)
                .Include(e => e.Contracts)
                .Where(e => e.Status == "Active")
                .ToListAsync();

            // 2. Lấy dữ liệu chấm công trong tháng
            var attendances = await _context.AttendanceDailies
                .Where(a => a.WorkDate >= startDate && a.WorkDate <= endDate)
                .ToListAsync();

            // 3. Lấy đơn từ nghỉ phép / công tác đã duyệt
            var approvedLeaves = await _context.LeaveRequests
                .Where(l => l.Status == "GIAM_DOC_DUYET" &&
                           ((l.FromDate >= startDate && l.FromDate <= endDate) ||
                            (l.ToDate >= startDate && l.ToDate <= endDate)))
                .ToListAsync();

            decimal totalGross = 0;
            decimal totalNet = 0;
            decimal totalInsurance = 0;
            decimal totalPIT = 0;

            foreach (var emp in employees)
            {
                var activeContract = emp.Contracts
                    .Where(c => c.Status == "Active")
                    .OrderByDescending(c => c.StartDate)
                    .FirstOrDefault();

                decimal baseSalary = activeContract?.BasicSalary ?? (emp.Position?.BaseSalary > 0 ? emp.Position.BaseSalary : 10000000m);
                decimal insSalary = activeContract?.InsuranceSalary ?? Math.Min(baseSalary, 36000000m); // Trần đóng BHXH 20 lần lương cơ sở

                // Thống kê chấm công
                var empAtts = attendances.Where(a => a.EmployeeId == emp.Id).ToList();
                decimal actualWorkDays = empAtts.Count(a => a.AttendanceStatus == "Present" || a.AttendanceStatus == "Late" || a.AttendanceStatus == "Early");
                
                // Nếu chưa có chấm công đủ tháng, tính theo số giờ làm hoặc gán mức tối thiểu hợp lý
                if (actualWorkDays == 0 && empAtts.Count == 0)
                {
                    actualWorkDays = standardDays; // Mặc định đủ công nếu chưa có máy chấm công
                }

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

                // Tính lương ngày công
                decimal dailyRate = standardDays > 0 ? (baseSalary / standardDays) : 0;
                decimal hourlyRate = dailyRate / 8m;

                decimal eligibleDays = Math.Min(actualWorkDays + paidLeaveDays, standardDays);
                decimal standardWorkSalary = Math.Round(dailyRate * eligibleDays, 0);

                // Tiền Overtime (hệ số 1.5)
                decimal overtimeSalary = Math.Round(hourlyRate * 1.5m * overtimeHours, 0);

                // Phụ cấp & Thưởng
                decimal posAllowanceRate = emp.Position?.AllowanceRate ?? 0;
                decimal positionAllowance = Math.Round(baseSalary * posAllowanceRate, 0);
                decimal lunchAllowance = 730000m; // Phụ cấp ăn trưa
                decimal otherAllowance = 500000m; // Phụ cấp xăng xe & điện thoại
                decimal bonus = emp.Position?.Code == "NV-KD" ? 2500000m : 1000000m;

                decimal grossSalary = standardWorkSalary + overtimeSalary + positionAllowance + lunchAllowance + otherAllowance + bonus;

                // Các khoản khấu trừ
                // Phạt đi muộn không phép (50,000đ cho mỗi 15 phút trễ)
                decimal latePenalty = (totalLateMinutes / 15) * 50000m;

                // Bảo hiểm bắt buộc: BHXH 8%, BHYT 1.5%, BHTN 1% = 10.5%
                decimal bhxh = Math.Round(insSalary * 0.08m, 0);
                decimal bhyt = Math.Round(insSalary * 0.015m, 0);
                decimal bhtn = Math.Round(insSalary * 0.01m, 0);
                decimal empTotalInsurance = bhxh + bhyt + bhtn;

                // Thuế TNCN (Personal Income Tax)
                // Giảm trừ bản thân: 11,000,000đ. Các khoản miễn thuế: Tiền ăn trưa, công tác phí.
                decimal nonTaxableIncome = lunchAllowance + otherAllowance + empTotalInsurance + 11000000m;
                decimal taxableIncome = Math.Max(0, grossSalary - nonTaxableIncome);
                decimal pit = CalculateProgressivePIT(taxableIncome);

                // Thực lĩnh (Net Salary)
                decimal netSalary = Math.Round(grossSalary - empTotalInsurance - pit - latePenalty, 0);

                totalGross += grossSalary;
                totalNet += netSalary;
                totalInsurance += empTotalInsurance;
                totalPIT += pit;

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
                slip.InsuranceSalary = insSalary;
                slip.StandardDays = standardDays;
                slip.ActualWorkDays = actualWorkDays;
                slip.PaidLeaveDays = paidLeaveDays;
                slip.UnpaidLeaveDays = unpaidLeaveDays;
                slip.OvertimeHours = overtimeHours;
                slip.StandardWorkSalary = standardWorkSalary;
                slip.OvertimeSalary = overtimeSalary;
                slip.PositionAllowance = positionAllowance;
                slip.LunchAllowance = lunchAllowance;
                slip.OtherAllowance = otherAllowance;
                slip.Bonus = bonus;
                slip.GrossSalary = grossSalary;
                slip.LatePenalty = latePenalty;
                slip.SocialInsurance = bhxh;
                slip.HealthInsurance = bhyt;
                slip.UnemploymentInsurance = bhtn;
                slip.TotalInsurance = empTotalInsurance;
                slip.TaxableIncome = taxableIncome;
                slip.PersonalIncomeTax = pit;
                slip.NetSalary = netSalary;
                slip.Status = "DRAFT";
                slip.Note = $"Đã tính tự động theo công chuẩn {standardDays} ngày.";
            }

            period.TotalGrossSalary = totalGross;
            period.TotalNetSalary = totalNet;
            period.TotalInsurance = totalInsurance;
            period.TotalPIT = totalPIT;

            await _context.SaveChangesAsync();
            return period;
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
                slip.Status = "SENT";
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

            ws.Cell(3, 1).Value = $"Công chuẩn: {period.StandardWorkDays} ngày | Trạng thái: {period.Status} | Xuất ngày: {DateTime.Now:dd/MM/yyyy HH:mm}";
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
                
                ws.Cell(rowIdx, 7).Value = (double)slip.BasicSalary;
                ws.Cell(rowIdx, 8).Value = slip.StandardDays;
                ws.Cell(rowIdx, 9).Value = (double)slip.ActualWorkDays;
                ws.Cell(rowIdx, 10).Value = (double)slip.PaidLeaveDays;
                ws.Cell(rowIdx, 11).Value = (double)slip.OvertimeHours;

                ws.Cell(rowIdx, 12).Value = (double)slip.StandardWorkSalary;
                ws.Cell(rowIdx, 13).Value = (double)slip.OvertimeSalary;
                ws.Cell(rowIdx, 14).Value = (double)slip.PositionAllowance;
                ws.Cell(rowIdx, 15).Value = (double)slip.LunchAllowance;
                ws.Cell(rowIdx, 16).Value = (double)slip.OtherAllowance;
                ws.Cell(rowIdx, 17).Value = (double)slip.Bonus;
                ws.Cell(rowIdx, 18).Value = (double)slip.GrossSalary;

                ws.Cell(rowIdx, 19).Value = (double)slip.SocialInsurance;
                ws.Cell(rowIdx, 20).Value = (double)slip.HealthInsurance;
                ws.Cell(rowIdx, 21).Value = (double)slip.UnemploymentInsurance;
                ws.Cell(rowIdx, 22).Value = (double)slip.TotalInsurance;
                ws.Cell(rowIdx, 23).Value = (double)slip.LatePenalty;
                ws.Cell(rowIdx, 24).Value = (double)slip.PersonalIncomeTax;
                ws.Cell(rowIdx, 25).Value = (double)slip.NetSalary;
                ws.Cell(rowIdx, 26).Value = slip.Status;
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

        // Thuật toán thuế TNCN biểu lũy tiến từng phần (Thông tư 111/2013/TT-BTC)
        private static decimal CalculateProgressivePIT(decimal taxableIncome)
        {
            if (taxableIncome <= 0) return 0;

            if (taxableIncome <= 5000000m)
                return taxableIncome * 0.05m;
            if (taxableIncome <= 10000000m)
                return (taxableIncome * 0.10m) - 250000m;
            if (taxableIncome <= 18000000m)
                return (taxableIncome * 0.15m) - 750000m;
            if (taxableIncome <= 32000000m)
                return (taxableIncome * 0.20m) - 1650000m;
            if (taxableIncome <= 52000000m)
                return (taxableIncome * 0.25m) - 3250000m;
            if (taxableIncome <= 80000000m)
                return (taxableIncome * 0.30m) - 5850000m;

            return (taxableIncome * 0.35m) - 9850000m;
        }
    }
}
