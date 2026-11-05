using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.Enums;
using ERP_HRM_CMS.Models.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP_HRM_CMS.Services.HRM
{
    public class SelfServicePortalService : ISelfServicePortalService
    {
        private readonly AppDbContext _context;
        private readonly ILeaveManagementService _leaveService;
        private readonly IAttendanceService _attendanceService;
        private readonly IShiftScheduleService _shiftService;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<SelfServicePortalService> _logger;

        public SelfServicePortalService(
            AppDbContext context,
            ILeaveManagementService leaveService,
            IAttendanceService attendanceService,
            IShiftScheduleService shiftService,
            IWebHostEnvironment env,
            ILogger<SelfServicePortalService> logger)
        {
            _context = context;
            _leaveService = leaveService;
            _attendanceService = attendanceService;
            _shiftService = shiftService;
            _env = env;
            _logger = logger;
        }

        // ═══════════════════════════════════════════════════════════
        // 1. DASHBOARD TỔNG QUAN BENTO
        // ═══════════════════════════════════════════════════════════

        public async Task<SelfServiceDashboardViewModel> GetDashboardAsync(
            int employeeId, int year, int month, bool isPayslipUnlocked)
        {
            if (year <= 2000) year = VietnamTime.Today.Year;
            if (month < 1 || month > 12) month = VietnamTime.Today.Month;

            var vm = new SelfServiceDashboardViewModel
            {
                Year = year,
                Month = month,
                IsPayslipUnlocked = isPayslipUnlocked
            };

            // 1. Hồ sơ nhân viên
            var emp = await _context.Employees
                .AsNoTracking()
                .Include(e => e.Department).ThenInclude(d => d.Branch)
                .Include(e => e.Position)
                .Include(e => e.EmployeeDetail)
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            if (emp == null) return vm;

            vm.Profile = new EmployeeProfileSummaryDto
            {
                Id = emp.Id,
                EmployeeCode = emp.EmployeeCode,
                FullName = emp.FullName,
                Email = emp.Email,
                Phone = emp.Phone,
                AvatarUrl = emp.AvatarUrl,
                DepartmentName = emp.Department?.Name ?? "Chưa phân bổ",
                PositionName = emp.Position?.Name ?? "Nhân viên",
                BranchName = emp.Department?.Branch?.Name ?? "Tổng công ty",
                HireDate = emp.HireDate,
                SeniorityYears = Math.Max(0, VietnamTime.Today.Year - emp.HireDate.Year),
                Status = emp.Status,

                CitizenId = emp.IdentityCard,
                TaxCode = emp.EmployeeDetail?.TaxIdentificationNumber,
                SocialInsuranceNumber = emp.EmployeeDetail?.SocialInsuranceNumber,
                BankAccountNumber = emp.EmployeeDetail?.BankAccountNumber,
                BankName = emp.EmployeeDetail?.BankName,
                BankBranch = emp.EmployeeDetail?.BankBranch,
                CurrentAddress = emp.EmployeeDetail?.CurrentAddress,
                PermanentAddress = emp.EmployeeDetail?.PermanentAddress,
                EmergencyContactName = emp.EmployeeDetail?.EmergencyContactName,
                EmergencyContactPhone = emp.EmployeeDetail?.EmergencyContactPhone
            };

            // 2. Chấm công hôm nay
            vm.TodayCheckIn = await GetTodayCheckInStatusAsync(employeeId);

            // 3. Lịch tháng & thống kê chấm công
            vm.AttendanceSummary = await BuildMonthlyAttendanceSummaryAsync(employeeId, year, month);

            // 4. Quỹ phép năm
            var bal = await _context.EmployeeLeaveBalances
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.EmployeeId == employeeId && b.Year == year);

            if (bal == null)
            {
                // Tự động khởi tạo quỹ phép nếu chưa có
                await _leaveService.InitializeOrRecalculateBalanceAsync(employeeId, year, null);
                bal = await _context.EmployeeLeaveBalances
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b => b.EmployeeId == employeeId && b.Year == year);
            }

            if (bal != null)
            {
                vm.LeaveBalance = new LeaveBalanceSummaryDto
                {
                    Year = year,
                    StandardDays = bal.StandardDays,
                    SeniorityBonusDays = bal.SeniorityBonusDays,
                    CarriedOverDays = bal.CarriedOverDays,
                    UsedDays = bal.UsedDays
                };
            }

            // 5. Đơn từ cá nhân gần đây (lấy 15 đơn mới nhất)
            var requests = await _context.LeaveRequests
                .AsNoTracking()
                .Where(l => l.EmployeeId == employeeId)
                .OrderByDescending(l => l.CreatedAt)
                .Take(15)
                .Include(l => l.DepartmentHead)
                .Include(l => l.Director)
                .ToListAsync();

            vm.LeaveRequests = requests.Select(r => new PersonalLeaveRequestDto
            {
                Id = r.Id,
                RequestCode = r.RequestCode,
                RequestType = r.RequestType,
                RequestTypeName = GetRequestTypeName(r.RequestType),
                FromDate = r.FromDate,
                ToDate = r.ToDate,
                TotalDays = r.TotalDays,
                Reason = r.Reason,
                AttachmentUrl = r.AttachmentUrl,
                Status = r.Status,
                StatusName = GetStatusName(r.Status),
                StatusBadgeClass = GetStatusBadgeClass(r.Status),
                CreatedAt = r.CreatedAt,
                DepartmentHeadName = r.DepartmentHead?.Username,
                DepartmentHeadApprovedAt = r.DepartmentHeadApprovedAt,
                DepartmentHeadNote = r.DepartmentHeadNote,
                DirectorName = r.Director?.Username,
                DirectorApprovedAt = r.DirectorApprovedAt,
                DirectorNote = r.DirectorNote
            }).ToList();

            // 6. Danh sách phiếu lương đã phát hành (loại trừ bản Draft)
            var slips = await _context.SalarySlips
                .AsNoTracking()
                .Include(s => s.PayrollPeriod)
                .Where(s => s.EmployeeId == employeeId && s.Status != SalarySlipStatus.Draft)
                .OrderByDescending(s => s.PayrollPeriod.Year)
                .ThenByDescending(s => s.PayrollPeriod.Month)
                .ToListAsync();

            vm.SalarySlips = slips.Select(s => new PersonalSalarySlipSummaryDto
            {
                Id = s.Id,
                PeriodName = s.PayrollPeriod != null ? s.PayrollPeriod.Name : $"Kỳ lương {s.CreatedAt:MM/yyyy}",
                Month = s.PayrollPeriod?.Month ?? s.CreatedAt.Month,
                Year = s.PayrollPeriod?.Year ?? s.CreatedAt.Year,
                StandardDays = s.StandardDays,
                ActualWorkDays = s.ActualWorkDays,
                NetSalary = s.NetSalary,
                Status = s.Status,
                StatusName = s.Status == SalarySlipStatus.Confirmed ? "Đã xác nhận" : "Đã phát hành",
                CreatedAt = s.CreatedAt
            }).ToList();

            // 7. Thiết bị / tài sản cấp phát đang dùng
            var assets = await _context.EmployeeAssetAssignments
                .AsNoTracking()
                .Include(a => a.Asset)
                .Where(a => a.EmployeeId == employeeId && a.Status == "Active")
                .OrderByDescending(a => a.AssignedDate)
                .ToListAsync();

            vm.Assets = assets.Select(a => new PersonalAssetDto
            {
                Id = a.Id,
                AssetCode = a.Asset?.AssetCode ?? $"TS-{a.AssetId}",
                AssetName = a.Asset?.AssetName ?? "Thiết bị công ty",
                Category = a.Asset?.AssetType ?? "Thiết bị",
                SerialNumber = a.Asset?.SerialNumber,
                AssignedDate = a.AssignedDate,
                ConditionOnAssign = a.ConditionOnAssign,
                Status = a.Status
            }).ToList();

            // 8. Hợp đồng lao động
            var contracts = await _context.Contracts
                .AsNoTracking()
                .Where(c => c.EmployeeId == employeeId)
                .OrderByDescending(c => c.StartDate)
                .ToListAsync();

            vm.Contracts = contracts.Select(c => new PersonalContractDto
            {
                Id = c.Id,
                ContractNumber = c.ContractNumber,
                ContractType = c.ContractType,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                BasicSalary = c.BasicSalary,
                Status = c.Status
            }).ToList();

            // 9. Khóa đào tạo nội bộ
            var trainings = await _context.TrainingParticipants
                .AsNoTracking()
                .Include(tp => tp.Training)
                .Where(tp => tp.EmployeeId == employeeId)
                .OrderByDescending(tp => tp.Training.StartDate)
                .Take(5)
                .ToListAsync();

            vm.Trainings = trainings.Select(tp => new PersonalTrainingDto
            {
                Id = tp.TrainingId,
                Code = tp.Training?.Code ?? $"TR-{tp.TrainingId}",
                Title = tp.Training?.Title ?? "Khóa đào tạo",
                TrainerName = tp.Training?.TrainerName,
                Location = tp.Training?.Location,
                StartDate = tp.Training?.StartDate ?? VietnamTime.Today,
                EndDate = tp.Training?.EndDate,
                Status = tp.Training?.Status ?? "Planned"
            }).ToList();

            // 10. Đánh giá KPI
            var kpis = await _context.KpiReviews
                .AsNoTracking()
                .Where(k => k.EmployeeId == employeeId)
                .OrderByDescending(k => k.PeriodYear)
                .ThenByDescending(k => k.PeriodMonth)
                .Take(5)
                .ToListAsync();

            vm.Kpis = kpis.Select(k => new PersonalKpiDto
            {
                Id = k.Id,
                PeriodMonth = k.PeriodMonth,
                PeriodYear = k.PeriodYear,
                TotalScore = k.TotalScore,
                Grade = k.Grade,
                OverallComment = k.OverallComment,
                Status = k.Status
            }).ToList();

            return vm;
        }

        // ═══════════════════════════════════════════════════════════
        // 2. CHẤM CÔNG TRỰC TUYẾN (WEB CHECK-IN / CHECK-OUT)
        // ═══════════════════════════════════════════════════════════

        public async Task<(bool Success, string? Error, TodayCheckInStatusDto? Status)> WebCheckInAsync(
            int employeeId, string? clientIp, string? userAgent, int? currentUserId)
        {
            var today = VietnamTime.Today;
            var nowTime = VietnamTime.Now.TimeOfDay;

            // Tìm ca làm việc hôm nay
            var shift = await _context.Shifts.FirstOrDefaultAsync(s => s.IsActive)
                        ?? new Shift { Id = 1, Name = "Ca hành chính", Code = "CA_HC", StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(17, 30, 0), IsActive = true };

            var existing = await _context.AttendanceDailies
                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.WorkDate.Date == today);

            if (existing != null && existing.FirstInTime.HasValue)
            {
                return (false, $"Bạn đã thực hiện Check-in hôm nay lúc {existing.FirstInTime.Value:hh\\:mm}. Không thể Check-in lại.", await GetTodayCheckInStatusAsync(employeeId));
            }

            // Tính ân hạn đi muộn (GracePeriodMinutes)
            int graceMinutes = shift.GracePeriodMinutes;
            var lateThreshold = shift.StartTime.Add(TimeSpan.FromMinutes(graceMinutes));

            int lateMinutes = 0;
            string status = "Present";

            if (nowTime > lateThreshold)
            {
                lateMinutes = (int)(nowTime - shift.StartTime).TotalMinutes;
                status = "Late";
            }

            if (existing == null)
            {
                existing = new AttendanceDaily
                {
                    EmployeeId = employeeId,
                    ShiftId = shift.Id,
                    WorkDate = today,
                    FirstInTime = nowTime,
                    LateMinutes = lateMinutes,
                    AttendanceStatus = status,
                    Note = $"Web Check-in [IP: {clientIp ?? "127.0.0.1"}]",
                    CreatedAt = VietnamTime.Now
                };
                _context.AttendanceDailies.Add(existing);
            }
            else
            {
                existing.ShiftId = shift.Id;
                existing.FirstInTime = nowTime;
                existing.LateMinutes = lateMinutes;
                existing.AttendanceStatus = status;
                existing.Note = string.IsNullOrWhiteSpace(existing.Note)
                    ? $"Web Check-in [IP: {clientIp ?? "127.0.0.1"}]"
                    : existing.Note + $" | Web Check-in [IP: {clientIp ?? "127.0.0.1"}]";
            }

            await _context.SaveChangesAsync();
            return (true, null, await GetTodayCheckInStatusAsync(employeeId));
        }

        public async Task<(bool Success, string? Error, TodayCheckInStatusDto? Status)> WebCheckOutAsync(
            int employeeId, string? clientIp, string? userAgent, int? currentUserId)
        {
            var today = VietnamTime.Today;
            var nowTime = VietnamTime.Now.TimeOfDay;

            var existing = await _context.AttendanceDailies
                .Include(a => a.Shift)
                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.WorkDate.Date == today);

            if (existing == null || !existing.FirstInTime.HasValue)
            {
                return (false, "Bạn chưa thực hiện Check-in hôm nay. Vui lòng Check-in trước khi Check-out.", await GetTodayCheckInStatusAsync(employeeId));
            }

            existing.LastOutTime = nowTime;

            // Tính số giờ làm việc thực tế
            var span = nowTime - existing.FirstInTime.Value;
            decimal hours = (decimal)Math.Max(0, span.TotalHours);
            // Trừ 1.5 giờ nghỉ trưa nếu ca làm việc trên 5 tiếng
            if (hours > 5.0m)
            {
                hours = Math.Max(0, hours - 1.5m);
            }
            existing.WorkHourCount = Math.Min(8.0m, Math.Round(hours, 2));

            // Tính về sớm nếu có ca kết thúc
            var shift = existing.Shift ?? await _context.Shifts.FirstOrDefaultAsync(s => s.IsActive);
            if (shift != null && nowTime < shift.EndTime)
            {
                existing.EarlyMinutes = (int)(shift.EndTime - nowTime).TotalMinutes;
            }
            else
            {
                existing.EarlyMinutes = 0;
            }

            // Tính tăng ca OT nếu làm việc vượt giờ kết thúc ca > 30 phút
            if (shift != null && nowTime > shift.EndTime.Add(TimeSpan.FromMinutes(30)))
            {
                var otSpan = nowTime - shift.EndTime;
                existing.OvertimeHours = Math.Round((decimal)otSpan.TotalHours, 1);
            }

            existing.Note = string.IsNullOrWhiteSpace(existing.Note)
                ? $"Web Check-out [IP: {clientIp ?? "127.0.0.1"}]"
                : existing.Note + $" | Web Check-out [IP: {clientIp ?? "127.0.0.1"}]";

            await _context.SaveChangesAsync();
            return (true, null, await GetTodayCheckInStatusAsync(employeeId));
        }

        private async Task<TodayCheckInStatusDto> GetTodayCheckInStatusAsync(int employeeId)
        {
            var today = VietnamTime.Today;
            var shift = await _context.Shifts.FirstOrDefaultAsync(s => s.IsActive)
                        ?? new Shift { Id = 1, Name = "Ca hành chính", Code = "CA_HC", StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(17, 30, 0), GracePeriodMinutes = 15 };

            var daily = await _context.AttendanceDailies
                .AsNoTracking()
                .Include(a => a.Shift)
                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.WorkDate.Date == today);

            var activeShift = daily?.Shift ?? shift;

            var dto = new TodayCheckInStatusDto
            {
                ShiftId = activeShift.Id,
                ShiftName = activeShift.Name,
                ShiftStartTime = activeShift.StartTime,
                ShiftEndTime = activeShift.EndTime,
                GracePeriodMinutes = activeShift.GracePeriodMinutes,
                HasCheckedIn = daily?.FirstInTime.HasValue ?? false,
                CheckInTime = daily?.FirstInTime,
                HasCheckedOut = daily?.LastOutTime.HasValue ?? false,
                CheckOutTime = daily?.LastOutTime,
                LateMinutes = daily?.LateMinutes ?? 0,
                EarlyMinutes = daily?.EarlyMinutes ?? 0,
                WorkHours = daily?.WorkHourCount ?? 0,
                OvertimeHours = daily?.OvertimeHours ?? 0
            };

            if (!dto.HasCheckedIn)
            {
                dto.StatusText = "Chưa chấm công";
                dto.BadgeClass = "badge-neutral";
            }
            else if (!dto.HasCheckedOut)
            {
                if (dto.LateMinutes > 0)
                {
                    dto.StatusText = $"Đang làm việc (Vào muộn {dto.LateMinutes}p)";
                    dto.BadgeClass = "badge-warning";
                }
                else
                {
                    dto.StatusText = "Đang làm việc (Đúng giờ)";
                    dto.BadgeClass = "badge-success";
                }
            }
            else
            {
                dto.StatusText = $"Đã hoàn thành ({dto.WorkHours:N1} giờ)";
                dto.BadgeClass = "badge-primary";
            }

            return dto;
        }

        // ═══════════════════════════════════════════════════════════
        // 3. LỊCH THÁNG & THỐNG KÊ CHẤM CÔNG CÁ NHÂN
        // ═══════════════════════════════════════════════════════════

        private async Task<AttendanceMonthSummaryDto> BuildMonthlyAttendanceSummaryAsync(
            int employeeId, int year, int month)
        {
            int daysInMonth = DateTime.DaysInMonth(year, month);
            var startDate = new DateTime(year, month, 1);
            var endDate = new DateTime(year, month, daysInMonth);

            var dailies = await _context.AttendanceDailies
                .AsNoTracking()
                .Include(a => a.Shift)
                .Where(a => a.EmployeeId == employeeId && a.WorkDate >= startDate && a.WorkDate <= endDate)
                .ToDictionaryAsync(a => a.WorkDate.Day);

            var leaves = await _context.LeaveRequests
                .AsNoTracking()
                .Where(l => l.EmployeeId == employeeId
                            && l.Status == LeaveRequestStatus.DirectorApproved
                            && l.FromDate <= endDate && l.ToDate >= startDate)
                .ToListAsync();

            var summary = new AttendanceMonthSummaryDto
            {
                Year = year,
                Month = month
            };

            for (int day = 1; day <= daysInMonth; day++)
            {
                var curDate = new DateTime(year, month, day);
                bool isWeekend = curDate.DayOfWeek == DayOfWeek.Saturday || curDate.DayOfWeek == DayOfWeek.Sunday;
                bool isFuture = curDate > VietnamTime.Today;

                var item = new PersonalDayAttendanceDto
                {
                    Day = day,
                    Date = curDate,
                    DayOfWeekName = GetDayOfWeekShort(curDate.DayOfWeek),
                    IsWeekend = isWeekend,
                    IsFuture = isFuture
                };

                if (dailies.TryGetValue(day, out var att))
                {
                    item.FirstInTime = att.FirstInTime;
                    item.LastOutTime = att.LastOutTime;
                    item.LateMinutes = att.LateMinutes;
                    item.EarlyMinutes = att.EarlyMinutes;
                    item.WorkHours = att.WorkHourCount;
                    item.OvertimeHours = att.OvertimeHours;
                    item.ShiftName = att.Shift?.Name ?? "Ca hành chính";

                    if (att.AttendanceStatus == "Leave")
                    {
                        item.StatusCode = "P";
                        item.StatusText = "Nghỉ phép";
                        item.BadgeClass = "badge-info";
                    }
                    else if (att.OvertimeHours > 0)
                    {
                        item.StatusCode = "OT";
                        item.StatusText = $"Tăng ca (+{att.OvertimeHours:N1}h)";
                        item.BadgeClass = "badge-primary";
                    }
                    else if (att.LateMinutes > 0 || att.EarlyMinutes > 0)
                    {
                        item.StatusCode = "M";
                        item.StatusText = $"Trễ {att.LateMinutes}p";
                        item.BadgeClass = "badge-warning";
                        item.CanAdjust = true;
                    }
                    else
                    {
                        item.StatusCode = "X";
                        item.StatusText = "Đủ công";
                        item.BadgeClass = "badge-success";
                    }

                    if (!att.LastOutTime.HasValue && !isFuture && curDate < VietnamTime.Today)
                    {
                        item.CanAdjust = true; // Thiếu giờ về trong quá khứ
                    }

                    summary.ActualWorkDays += att.WorkHourCount >= 4.0m ? (att.WorkHourCount >= 7.0m ? 1.0m : 0.5m) : 0m;
                    summary.TotalWorkHours += att.WorkHourCount;
                    summary.OvertimeHours += att.OvertimeHours;
                    if (att.LateMinutes > 0)
                    {
                        summary.LateCount++;
                        summary.TotalLateMinutes += att.LateMinutes;
                    }
                    if (att.EarlyMinutes > 0) summary.EarlyCount++;
                }
                else
                {
                    // Kiểm tra có đơn xin nghỉ phép đã duyệt cho ngày này không
                    bool hasApprovedLeave = leaves.Any(l => l.FromDate.Date <= curDate && l.ToDate.Date >= curDate);
                    if (hasApprovedLeave)
                    {
                        item.StatusCode = "P";
                        item.StatusText = "Nghỉ phép";
                        item.BadgeClass = "badge-info";
                        summary.PaidLeaveDays += 1.0m;
                    }
                    else if (isWeekend)
                    {
                        item.StatusCode = "-";
                        item.StatusText = "Cuối tuần";
                        item.BadgeClass = "badge-neutral";
                    }
                    else if (isFuture)
                    {
                        item.StatusCode = "-";
                        item.StatusText = "Chưa đến";
                        item.BadgeClass = "badge-neutral";
                    }
                    else
                    {
                        // Ngày làm việc trong quá khứ không có chấm công
                        item.StatusCode = "KL";
                        item.StatusText = "Vắng mặt";
                        item.BadgeClass = "badge-danger";
                        item.CanAdjust = true; // Cho phép gửi giải trình
                    }
                }

                summary.Days.Add(item);
            }

            // Tính số ngày công chuẩn trong tháng (loại trừ Thứ 7, Chủ nhật)
            summary.StandardDays = summary.Days.Count(d => !d.IsWeekend);
            return summary;
        }

        // ═══════════════════════════════════════════════════════════
        // 4. QUẢN LÝ ĐƠN TỪ CÁ NHÂN (TẠO / HỦY)
        // ═══════════════════════════════════════════════════════════

        public async Task<(bool Success, string? Error, int RequestId)> CreateLeaveRequestAsync(
            PersonalLeaveRequestCreateDto dto, int employeeId, int currentUserId)
        {
            if (dto.FromDate.Date > dto.ToDate.Date)
            {
                return (false, "Từ ngày không thể lớn hơn đến ngày.", 0);
            }

            string? attachmentUrl = null;
            if (dto.AttachmentFile != null && dto.AttachmentFile.Length > 0)
            {
                attachmentUrl = await SaveAttachmentAsync(dto.AttachmentFile, "leaverequests");
            }

            decimal totalDays = dto.TotalDays > 0
                ? dto.TotalDays
                : (decimal)(dto.ToDate.Date - dto.FromDate.Date).TotalDays + 1;

            var req = new LeaveRequest
            {
                EmployeeId = employeeId, // RÀNG BUỘC THEO EMPLOYEE ĐANG ĐĂNG NHẬP (ZERO IDOR)
                RequestType = dto.RequestType,
                FromDate = dto.FromDate.Date,
                ToDate = dto.ToDate.Date,
                TotalDays = totalDays,
                Reason = dto.Reason,
                AttachmentUrl = attachmentUrl
            };

            return await _leaveService.CreateLeaveRequestAsync(req, currentUserId);
        }

        public async Task<(bool Success, string? Error)> CancelLeaveRequestAsync(
            int requestId, int employeeId, int currentUserId)
        {
            var req = await _context.LeaveRequests.FirstOrDefaultAsync(l => l.Id == requestId);
            if (req == null) return (false, "Không tìm thấy đơn từ.");

            // KIỂM SOÁT ZERO IDOR: Tuyệt đối không cho phép hủy đơn của người khác
            if (req.EmployeeId != employeeId)
            {
                return (false, "Vi phạm kiểm soát truy cập: Bạn không có quyền thao tác trên đơn từ của nhân viên khác.");
            }

            if (req.Status != LeaveRequestStatus.PendingApproval)
            {
                return (false, "Chỉ có thể hủy (rút lại) đơn từ khi đang ở trạng thái 'Chờ duyệt'. Các đơn đã qua phê duyệt hoặc đã từ chối không thể hủy.");
            }

            // Hủy đơn và ghi chú rõ ràng
            req.Status = LeaveRequestStatus.Rejected;
            req.DirectorNote = "Nhân viên đã tự rút lại (Hủy) đơn từ cổng ESS Portal.";

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error, int RequestId)> CreateAdjustmentRequestAsync(
            PersonalAdjustmentCreateDto dto, int employeeId, int currentUserId)
        {
            string? proofUrl = null;
            if (dto.AttachmentFile != null && dto.AttachmentFile.Length > 0)
            {
                proofUrl = await SaveAttachmentAsync(dto.AttachmentFile, "adjustments");
            }

            var shift = await _context.Shifts.FirstOrDefaultAsync(s => s.IsActive)
                        ?? new Shift { Id = 1, Name = "Ca hành chính", Code = "CA_HC" };

            var adj = new AttendanceAdjustmentRequest
            {
                EmployeeId = employeeId, // ZERO IDOR
                ShiftId = shift.Id,
                WorkDate = dto.WorkDate.Date,
                AdjustmentType = dto.AdjustmentType,
                ActualInTime = dto.ActualInTime,
                ActualOutTime = dto.ActualOutTime,
                Reason = dto.Reason,
                AttachmentUrl = proofUrl,
                Status = "Pending",
                CreatedAt = VietnamTime.Now
            };

            return await _leaveService.CreateAdjustmentRequestAsync(adj, currentUserId);
        }

        // ═══════════════════════════════════════════════════════════
        // 5. TRA CỨU PHIẾU LƯƠNG BẢO MẬT (SECURE PAYSLIP VIEWER)
        // ═══════════════════════════════════════════════════════════

        public async Task<bool> VerifyPasswordAsync(int userId, string password)
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null || string.IsNullOrWhiteSpace(password)) return false;

            return MatKhauHelper.Verify(password, user.PasswordHash, user.PasswordSalt);
        }

        public async Task<PersonalSalarySlipDetailDto?> GetSalarySlipDetailAsync(int slipId, int employeeId)
        {
            // RÀNG BUỘC NGHIÊM NGẶT Zero IDOR: s.EmployeeId == employeeId
            var s = await _context.SalarySlips
                .AsNoTracking()
                .Include(x => x.PayrollPeriod)
                .Include(x => x.Employee).ThenInclude(e => e.Department)
                .Include(x => x.Employee).ThenInclude(e => e.Position)
                .FirstOrDefaultAsync(x => x.Id == slipId && x.EmployeeId == employeeId);

            if (s == null) return null;

            return new PersonalSalarySlipDetailDto
            {
                Id = s.Id,
                PeriodName = s.PayrollPeriod != null ? s.PayrollPeriod.Name : $"Kỳ lương {s.CreatedAt:MM/yyyy}",
                Month = s.PayrollPeriod?.Month ?? s.CreatedAt.Month,
                Year = s.PayrollPeriod?.Year ?? s.CreatedAt.Year,
                EmployeeName = s.Employee?.FullName ?? "Nhân viên",
                EmployeeCode = s.Employee?.EmployeeCode ?? string.Empty,
                DepartmentName = s.Employee?.Department?.Name ?? "N/A",
                PositionName = s.Employee?.Position?.Name ?? "N/A",

                StandardDays = s.StandardDays,
                ActualWorkDays = s.ActualWorkDays,
                PaidLeaveDays = s.PaidLeaveDays,
                UnpaidLeaveDays = s.UnpaidLeaveDays,
                OvertimeHours = s.OvertimeHours,

                BasicSalary = s.BasicSalary,
                InsuranceSalary = s.InsuranceSalary,
                StandardWorkSalary = s.StandardWorkSalary,
                OvertimeSalary = s.OvertimeSalary,
                PositionAllowance = s.PositionAllowance,
                LunchAllowance = s.LunchAllowance,
                OtherAllowance = s.OtherAllowance,
                Bonus = s.Bonus,
                GrossSalary = s.GrossSalary,

                LatePenalty = s.LatePenalty,
                SocialInsurance = s.SocialInsurance,
                HealthInsurance = s.HealthInsurance,
                UnemploymentInsurance = s.UnemploymentInsurance,
                TotalInsurance = s.TotalInsurance,

                DependentCount = s.DependentCount,
                PersonalDeduction = s.PersonalDeduction,
                DependentDeduction = s.DependentDeduction,
                TaxableIncome = s.TaxableIncome,
                PersonalIncomeTax = s.PersonalIncomeTax,

                NetSalary = s.NetSalary,
                Status = s.Status == SalarySlipStatus.Confirmed ? "Đã xác nhận" : "Đã phát hành",
                Note = s.Note,
                CreatedAt = s.CreatedAt
            };
        }

        // ═══════════════════════════════════════════════════════════
        // 6. CẬP NHẬT THÔNG TIN LIÊN HỆ & ĐĂNG KÝ NGƯỜI PHỤ THUỘC
        // ═══════════════════════════════════════════════════════════

        public async Task<(bool Success, string? Error)> UpdateContactInfoAsync(
            PersonalContactUpdateDto dto, int employeeId, int currentUserId)
        {
            var emp = await _context.Employees
                .Include(e => e.EmployeeDetail)
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            if (emp == null) return (false, "Không tìm thấy hồ sơ nhân viên.");

            if (!string.IsNullOrWhiteSpace(dto.Phone)) emp.Phone = dto.Phone.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Email)) emp.Email = dto.Email.Trim();

            if (emp.EmployeeDetail == null)
            {
                emp.EmployeeDetail = new EmployeeDetail
                {
                    EmployeeId = employeeId,
                    CurrentAddress = dto.CurrentAddress,
                    PermanentAddress = dto.PermanentAddress,
                    EmergencyContactName = dto.EmergencyContactName,
                    EmergencyContactPhone = dto.EmergencyContactPhone
                };
                _context.EmployeeDetails.Add(emp.EmployeeDetail);
            }
            else
            {
                emp.EmployeeDetail.CurrentAddress = dto.CurrentAddress;
                emp.EmployeeDetail.PermanentAddress = dto.PermanentAddress;
                emp.EmployeeDetail.EmergencyContactName = dto.EmergencyContactName;
                emp.EmployeeDetail.EmergencyContactPhone = dto.EmergencyContactPhone;
            }

            emp.UpdatedAt = VietnamTime.Now;
            emp.UpdatedBy = "SelfService";

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> RegisterDependentAsync(
            PersonalDependentRegisterDto dto, int employeeId, int currentUserId)
        {
            if (string.IsNullOrWhiteSpace(dto.FullName))
                return (false, "Họ và tên người phụ thuộc không được để trống.");

            var dep = new EmployeeDependent
            {
                EmployeeId = employeeId, // ZERO IDOR
                FullName = dto.FullName.Trim(),
                Relationship = dto.Relationship.Trim(),
                DateOfBirth = dto.DateOfBirth,
                FromDate = dto.FromDate.Date
            };

            _context.EmployeeDependents.Add(dep);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<Employee?> GetEmployeeProfileAsync(int employeeId)
        {
            return await _context.Employees
                .AsNoTracking()
                .Include(e => e.Department).ThenInclude(d => d.Branch)
                .Include(e => e.Position)
                .FirstOrDefaultAsync(e => e.Id == employeeId);
        }

        // ═══════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════

        private async Task<string> SaveAttachmentAsync(Microsoft.AspNetCore.Http.IFormFile file, string folder)
        {
            string webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            string targetDir = Path.Combine(webRoot, "uploads", folder);
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            string fileName = $"{Guid.NewGuid():N}{ext}";
            string filePath = Path.Combine(targetDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/{folder}/{fileName}";
        }

        private static string GetRequestTypeName(string code) => code switch
        {
            "ANNUAL_LEAVE" => "Nghỉ phép năm",
            "SICK_LEAVE" => "Nghỉ ốm đau",
            "MATERNITY_LEAVE" => "Nghỉ thai sản",
            "UNPAID_LEAVE" => "Nghỉ không lương",
            "OVERTIME" => "Đăng ký làm thêm (OT)",
            "BUSINESS_TRIP" => "Đăng ký đi công tác",
            _ => code
        };

        private static string GetStatusName(LeaveRequestStatus status) => status switch
        {
            LeaveRequestStatus.PendingApproval => "Chờ duyệt",
            LeaveRequestStatus.DepartmentApproved => "Trưởng phòng đã duyệt",
            LeaveRequestStatus.DirectorApproved => "Đã duyệt chính thức",
            LeaveRequestStatus.Rejected => "Đã từ chối / Hủy",
            _ => status.ToString()
        };

        private static string GetStatusBadgeClass(LeaveRequestStatus status) => status switch
        {
            LeaveRequestStatus.PendingApproval => "badge-warning",
            LeaveRequestStatus.DepartmentApproved => "badge-info",
            LeaveRequestStatus.DirectorApproved => "badge-success",
            LeaveRequestStatus.Rejected => "badge-danger",
            _ => "badge-neutral"
        };

        private static string GetDayOfWeekShort(DayOfWeek dow) => dow switch
        {
            DayOfWeek.Monday => "T2",
            DayOfWeek.Tuesday => "T3",
            DayOfWeek.Wednesday => "T4",
            DayOfWeek.Thursday => "T5",
            DayOfWeek.Friday => "T6",
            DayOfWeek.Saturday => "T7",
            DayOfWeek.Sunday => "CN",
            _ => string.Empty
        };
    }
}

