using ClosedXML.Excel;
using ERP.Application.Common.Interfaces;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.Enums;
using ERP_HRM_CMS.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace ERP_HRM_CMS.Services.HRM
{
    public class AttendanceService : IAttendanceService
    {
        private readonly AppDbContext _context;
        private readonly IDataScopeService? _dataScope;

        public AttendanceService(AppDbContext context, IDataScopeService? dataScope = null)
        {
            _context = context;
            _dataScope = dataScope;
        }

        public async Task<PaginatedList<AttendanceDaily>> GetDailyAttendancePagedAsync(AttendanceDailyFilterDto filter)
        {
            var query = _context.AttendanceDailies
                .Include(a => a.Employee)
                    .ThenInclude(e => e.Department)
                .Include(a => a.Shift)
                .Where(a => a.WorkDate.Date == filter.SelectedDate.Date)
                .AsNoTracking()
                .AsQueryable();

            if (_dataScope != null)
                query = _dataScope.ApplyAttendanceScope(query);

            if (filter.DepartmentId.HasValue && filter.DepartmentId > 0)
                query = query.Where(a => a.Employee.DepartmentId == filter.DepartmentId.Value);

            if (filter.BranchId.HasValue && filter.BranchId > 0)
                query = query.Where(a => a.Employee.Department.BranchId == filter.BranchId.Value);

            if (!string.IsNullOrWhiteSpace(filter.Status))
                query = query.Where(a => a.AttendanceStatus == filter.Status);

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var kw = filter.Keyword.Trim().ToLower();
                query = query.Where(a => a.Employee.EmployeeCode.ToLower().Contains(kw) ||
                                         a.Employee.FullName.ToLower().Contains(kw));
            }

            query = query.OrderBy(a => a.Employee.EmployeeCode);
            return await PaginatedList<AttendanceDaily>.CreateAsync(query, filter.Page, filter.PageSize);
        }

        public async Task<(bool Success, string? Error)> QuickCheckInAsync(QuickCheckInDto dto, int? currentUserId)
        {
            var today = VietnamTime.Today;

            // Kiểm tra khóa kỳ công
            if (await IsPeriodLockedAsync(today.Year, today.Month))
                return (false, $"Kỳ chấm công tháng {today.Month}/{today.Year} đã bị khóa. Không thể ghi nhận chấm công.");

            var shift = await _context.Shifts.FindAsync(dto.ShiftId);
            if (shift == null) return (false, "Không tìm thấy ca làm việc.");

            var att = await _context.AttendanceDailies
                .FirstOrDefaultAsync(a => a.EmployeeId == dto.EmployeeId && a.WorkDate.Date == today);

            var currentTime = VietnamTime.Now.TimeOfDay;

            if (att == null)
            {
                int lateMin = 0;
                if (currentTime > shift.StartTime)
                {
                    lateMin = (int)(currentTime - shift.StartTime).TotalMinutes;
                    if (lateMin <= shift.GracePeriodMinutes)
                        lateMin = 0; // Trong thời gian ân hạn
                }

                att = new AttendanceDaily
                {
                    EmployeeId = dto.EmployeeId,
                    ShiftId = dto.ShiftId,
                    WorkDate = today,
                    FirstInTime = currentTime,
                    LateMinutes = lateMin,
                    AttendanceStatus = (lateMin > 0) ? "Late" : "Present",
                    CreatedAt = VietnamTime.Now
                };
                _context.AttendanceDailies.Add(att);
            }
            else
            {
                att.LastOutTime = currentTime;

                int earlyMin = 0;
                if (currentTime < shift.EndTime && !shift.IsOvernight)
                {
                    earlyMin = (int)(shift.EndTime - currentTime).TotalMinutes;
                }
                att.EarlyMinutes = earlyMin;

                if (att.FirstInTime.HasValue)
                {
                    var totalTime = (currentTime - att.FirstInTime.Value).TotalHours;
                    att.WorkHourCount = Math.Min((decimal)totalTime, shift.WorkHours);

                    if (currentTime > shift.EndTime && !shift.IsOvernight)
                    {
                        att.OvertimeHours = (decimal)(currentTime - shift.EndTime).TotalHours;
                    }
                }

                att.AttendanceStatus = (att.LateMinutes > 0 || att.EarlyMinutes > 0) ? "Late" : "Present";
            }

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<MonthlyTimesheetViewModel> GetMonthlyTimesheetAsync(
            int year, int month, int? departmentId, int? branchId)
        {
            int daysInMonth = DateTime.DaysInMonth(year, month);
            var startDate = new DateTime(year, month, 1);
            var endDate = new DateTime(year, month, daysInMonth);

            var empQuery = _context.Employees
                .Include(e => e.Department)
                .Where(e => e.Status == "Active")
                .AsNoTracking()
                .AsQueryable();

            if (_dataScope != null)
                empQuery = _dataScope.ApplyEmployeeScope(empQuery);

            if (departmentId.HasValue && departmentId > 0)
                empQuery = empQuery.Where(e => e.DepartmentId == departmentId.Value);

            if (branchId.HasValue && branchId > 0)
                empQuery = empQuery.Where(e => e.Department.BranchId == branchId.Value);

            var employees = await empQuery.OrderBy(e => e.EmployeeCode).ToListAsync();

            var attendances = await _context.AttendanceDailies
                .Include(a => a.Shift)
                .Where(a => a.WorkDate >= startDate && a.WorkDate <= endDate)
                .AsNoTracking()
                .ToListAsync();

            var approvedLeaves = await _context.LeaveRequests
                .Where(l => l.Status == LeaveRequestStatus.DirectorApproved &&
                            l.FromDate <= endDate && l.ToDate >= startDate)
                .AsNoTracking()
                .ToListAsync();

            var rows = new List<MonthlyEmployeeRowDto>();

            foreach (var emp in employees)
            {
                var row = new MonthlyEmployeeRowDto
                {
                    EmployeeId = emp.Id,
                    EmployeeCode = emp.EmployeeCode,
                    FullName = emp.FullName,
                    DepartmentName = emp.Department?.Name ?? "Chưa gán",
                    TotalWorkHours = 0,
                    TotalLateMinutes = 0,
                    PresentDaysCount = 0
                };

                var empAtts = attendances.Where(a => a.EmployeeId == emp.Id).ToList();
                var empLeaves = approvedLeaves.Where(l => l.EmployeeId == emp.Id).ToList();

                for (int day = 1; day <= daysInMonth; day++)
                {
                    var curDate = new DateTime(year, month, day);
                    var att = empAtts.FirstOrDefault(a => a.WorkDate.Day == day);
                    var leave = empLeaves.FirstOrDefault(l => curDate >= l.FromDate.Date && curDate <= l.ToDate.Date);

                    string badge = "x";
                    if (att != null)
                    {
                        if (att.AttendanceStatus == "Present") badge = "X";
                        else if (att.AttendanceStatus == "Late" || att.AttendanceStatus == "Early") badge = "M";
                        else if (att.AttendanceStatus == "Leave") badge = "P";
                        else if (att.AttendanceStatus == "BusinessTrip") badge = "CT";
                        else if (att.AttendanceStatus == "Absent") badge = "KL";
                        else badge = "X";

                        if (att.OvertimeHours > 0) badge += "+OT";

                        row.TotalWorkHours += att.WorkHourCount;
                        row.TotalLateMinutes += att.LateMinutes + att.EarlyMinutes;
                        if (att.AttendanceStatus != "Absent") row.PresentDaysCount++;
                    }
                    else if (leave != null)
                    {
                        badge = leave.RequestType == "UNPAID_LEAVE" ? "KL" : "P";
                    }
                    else
                    {
                        badge = (curDate.DayOfWeek == DayOfWeek.Sunday || curDate.DayOfWeek == DayOfWeek.Saturday) ? "-" : "x";
                    }

                    row.DailyStatuses[day] = badge;
                }

                rows.Add(row);
            }

            return new MonthlyTimesheetViewModel
            {
                Year = year,
                Month = month,
                DepartmentId = departmentId,
                DaysInMonth = daysInMonth,
                Rows = rows
            };
        }

        public async Task<DayAttendanceDetailDto?> GetDayAttendanceDetailAsync(int employeeId, DateTime date)
        {
            var emp = await _context.Employees
                .Include(e => e.Department)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            if (emp == null) return null;

            var att = await _context.AttendanceDailies
                .Include(a => a.Shift)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.WorkDate.Date == date.Date);

            var leave = await _context.LeaveRequests
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.EmployeeId == employeeId &&
                                          date.Date >= l.FromDate.Date && date.Date <= l.ToDate.Date);

            var adj = await _context.AttendanceAdjustmentRequests
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.EmployeeId == employeeId && r.WorkDate.Date == date.Date);

            var shift = att?.Shift ?? await _context.Shifts.FirstOrDefaultAsync(s => s.IsActive);

            return new DayAttendanceDetailDto
            {
                EmployeeId = emp.Id,
                EmployeeCode = emp.EmployeeCode,
                FullName = emp.FullName,
                DepartmentName = emp.Department?.Name ?? "",
                WorkDate = date,
                ShiftId = shift?.Id,
                ShiftName = shift?.Name ?? "Chưa gán ca",
                ShiftHours = shift != null ? $"{shift.StartTime:hh\\:mm} - {shift.EndTime:hh\\:mm} ({shift.WorkHours}h)" : "—",
                FirstInTime = att?.FirstInTime,
                LastOutTime = att?.LastOutTime,
                LateMinutes = att?.LateMinutes ?? 0,
                EarlyMinutes = att?.EarlyMinutes ?? 0,
                WorkHourCount = att?.WorkHourCount ?? 0,
                OvertimeHours = att?.OvertimeHours ?? 0,
                AttendanceStatus = att?.AttendanceStatus ?? (date.DayOfWeek == DayOfWeek.Sunday ? "Off" : "NoData"),
                Note = att?.Note,
                RelatedLeaveInfo = leave != null ? $"{leave.RequestCode}: {leave.RequestType} ({leave.Status})" : null,
                RelatedAdjustmentInfo = adj != null ? $"{adj.RequestCode}: {adj.AdjustmentType} ({adj.Status})" : null
            };
        }

        public async Task<byte[]> ExportMonthlyTimesheetToExcelAsync(
            int year, int month, int? departmentId, int? branchId)
        {
            var vm = await GetMonthlyTimesheetAsync(year, month, departmentId, branchId);
            int days = vm.DaysInMonth;

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add($"Bảng công T{month}_{year}");

            // Header Công ty
            ws.Cell("A1").Value = "CÔNG TY CỔ PHẦN CÔNG NGHỆ & THƯƠNG MẠI VIỆT NAM";
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 12;

            ws.Cell("A2").Value = $"BẢNG CHẤM CÔNG TỔNG HỢP THÁNG {month} NĂM {year}";
            ws.Cell("A2").Style.Font.Bold = true;
            ws.Cell("A2").Style.Font.FontSize = 15;
            ws.Cell("A2").Style.Font.FontColor = XLColor.Navy;

            // Bảng chính: Dòng 4
            int headerRow = 4;
            ws.Cell(headerRow, 1).Value = "STT";
            ws.Cell(headerRow, 2).Value = "Mã NV";
            ws.Cell(headerRow, 3).Value = "Họ và tên";
            ws.Cell(headerRow, 4).Value = "Phòng ban";

            for (int d = 1; d <= days; d++)
            {
                ws.Cell(headerRow, 4 + d).Value = d.ToString();
            }

            int colTotalWork = 5 + days;
            int colTotalLate = 6 + days;
            int colTotalDays = 7 + days;

            ws.Cell(headerRow, colTotalWork).Value = "Tổng giờ";
            ws.Cell(headerRow, colTotalLate).Value = "Trễ (phút)";
            ws.Cell(headerRow, colTotalDays).Value = "Số ngày công";

            var headerRange = ws.Range(headerRow, 1, headerRow, colTotalDays);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E293B");
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            int currentRow = headerRow + 1;
            int stt = 1;
            foreach (var r in vm.Rows)
            {
                ws.Cell(currentRow, 1).Value = stt++;
                ws.Cell(currentRow, 2).Value = r.EmployeeCode;
                ws.Cell(currentRow, 3).Value = r.FullName;
                ws.Cell(currentRow, 4).Value = r.DepartmentName;

                for (int d = 1; d <= days; d++)
                {
                    string status = r.DailyStatuses.ContainsKey(d) ? r.DailyStatuses[d] : "-";
                    var cell = ws.Cell(currentRow, 4 + d);
                    cell.Value = status;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    if (status == "X") cell.Style.Font.FontColor = XLColor.FromHtml("#15803D");
                    else if (status.StartsWith("M")) cell.Style.Font.FontColor = XLColor.FromHtml("#B45309");
                    else if (status == "P") cell.Style.Font.FontColor = XLColor.FromHtml("#2563EB");
                    else if (status == "KL") cell.Style.Font.FontColor = XLColor.FromHtml("#DC2626");
                }

                ws.Cell(currentRow, colTotalWork).Value = r.TotalWorkHours;
                ws.Cell(currentRow, colTotalLate).Value = r.TotalLateMinutes;
                ws.Cell(currentRow, colTotalDays).Value = r.PresentDaysCount;

                currentRow++;
            }

            // Ký tên đóng dấu
            int signRow = currentRow + 2;
            ws.Cell(signRow, 2).Value = "NGƯỜI LẬP BIỂU";
            ws.Cell(signRow, 2).Style.Font.Bold = true;

            ws.Cell(signRow, days / 2).Value = "KẾ TOÁN TRƯỞNG";
            ws.Cell(signRow, days / 2).Style.Font.Bold = true;

            ws.Cell(signRow, colTotalDays - 2).Value = "GIÁM ĐỐC DUYỆT";
            ws.Cell(signRow, colTotalDays - 2).Style.Font.Bold = true;

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        public async Task<AttendanceImportResultDto> ImportAttendanceLogAsync(
            Stream fileStream, string fileName, int? defaultShiftId, int? currentUserId)
        {
            var result = new AttendanceImportResultDto { Success = true };

            using var workbook = new XLWorkbook(fileStream);
            var ws = workbook.Worksheets.FirstOrDefault();
            if (ws == null)
            {
                result.Success = false;
                result.ErrorMessages.Add("File Excel rỗng, không tìm thấy Sheet dữ liệu nào.");
                return result;
            }

            var defaultShift = (defaultShiftId.HasValue ? await _context.Shifts.FindAsync(defaultShiftId.Value) : null)
                               ?? await _context.Shifts.FirstOrDefaultAsync(s => s.IsActive);

            if (defaultShift == null)
            {
                result.Success = false;
                result.ErrorMessages.Add("Hệ thống chưa có định nghĩa Ca làm việc nào để đối chiếu.");
                return result;
            }

            var employees = await _context.Employees
                .Where(e => e.Status == "Active")
                .ToDictionaryAsync(e => e.EmployeeCode.Trim().ToUpper(), e => e);

            var locks = await _context.AttendanceLocks
                .Where(l => l.IsLocked)
                .ToListAsync();

            var rows = ws.RowsUsed().Skip(1); // Bỏ qua tiêu đề
            int rowIdx = 1;

            foreach (var row in rows)
            {
                rowIdx++;
                string empCode = row.Cell(1).GetString().Trim().ToUpper();
                string dateStr = row.Cell(2).GetString().Trim();
                string inStr = row.Cell(3).GetString().Trim();
                string outStr = row.Cell(4).GetString().Trim();

                if (string.IsNullOrEmpty(empCode)) continue;
                result.TotalRows++;

                if (!employees.TryGetValue(empCode, out var emp))
                {
                    result.ErrorCount++;
                    result.ErrorMessages.Add($"Dòng {rowIdx}: Không tìm thấy nhân viên có mã '{empCode}'.");
                    continue;
                }

                if (!DateTime.TryParse(dateStr, CultureInfo.GetCultureInfo("vi-VN"), DateTimeStyles.None, out var workDate) &&
                    !DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out workDate))
                {
                    result.ErrorCount++;
                    result.ErrorMessages.Add($"Dòng {rowIdx}: Ngày '{dateStr}' không đúng định dạng.");
                    continue;
                }

                // Kiểm tra khóa kỳ
                if (locks.Any(l => l.Year == workDate.Year && l.Month == workDate.Month))
                {
                    result.ErrorCount++;
                    result.ErrorMessages.Add($"Dòng {rowIdx}: Kỳ chấm công tháng {workDate.Month}/{workDate.Year} đã bị khóa.");
                    continue;
                }

                TimeSpan? firstIn = null;
                if (!string.IsNullOrEmpty(inStr) && TimeSpan.TryParse(inStr, out var inTs))
                    firstIn = inTs;

                TimeSpan? lastOut = null;
                if (!string.IsNullOrEmpty(outStr) && TimeSpan.TryParse(outStr, out var outTs))
                    lastOut = outTs;

                var att = await _context.AttendanceDailies
                    .Include(a => a.Shift)
                    .FirstOrDefaultAsync(a => a.EmployeeId == emp.Id && a.WorkDate.Date == workDate.Date);

                var shift = att?.Shift ?? defaultShift;

                int lateMin = 0;
                if (firstIn.HasValue && firstIn.Value > shift.StartTime)
                {
                    lateMin = (int)(firstIn.Value - shift.StartTime).TotalMinutes;
                    if (lateMin <= shift.GracePeriodMinutes)
                        lateMin = 0;
                }

                int earlyMin = 0;
                if (lastOut.HasValue && lastOut.Value < shift.EndTime && !shift.IsOvernight)
                {
                    earlyMin = (int)(shift.EndTime - lastOut.Value).TotalMinutes;
                }

                decimal workHours = 0;
                decimal otHours = 0;
                if (firstIn.HasValue && lastOut.HasValue)
                {
                    var totalTime = (lastOut.Value - firstIn.Value).TotalHours;
                    workHours = Math.Min((decimal)totalTime, shift.WorkHours);

                    if (lastOut.Value > shift.EndTime && !shift.IsOvernight)
                        otHours = (decimal)(lastOut.Value - shift.EndTime).TotalHours;
                }
                else if (firstIn.HasValue)
                {
                    workHours = shift.WorkHours / 2; // Đi làm nửa buổi
                }

                string status = (lateMin > 0 || earlyMin > 0) ? "Late" : (workHours > 0 ? "Present" : "Absent");

                if (att == null)
                {
                    _context.AttendanceDailies.Add(new AttendanceDaily
                    {
                        EmployeeId = emp.Id,
                        ShiftId = shift.Id,
                        WorkDate = workDate.Date,
                        FirstInTime = firstIn,
                        LastOutTime = lastOut,
                        LateMinutes = lateMin,
                        EarlyMinutes = earlyMin,
                        WorkHourCount = workHours,
                        OvertimeHours = otHours,
                        AttendanceStatus = status,
                        Note = "[Import máy chấm công]",
                        CreatedAt = VietnamTime.Now
                    });
                }
                else
                {
                    att.FirstInTime = firstIn ?? att.FirstInTime;
                    att.LastOutTime = lastOut ?? att.LastOutTime;
                    att.LateMinutes = lateMin;
                    att.EarlyMinutes = earlyMin;
                    att.WorkHourCount = workHours;
                    att.OvertimeHours = otHours;
                    att.AttendanceStatus = status;
                    att.Note = "[Import máy chấm công]";
                }

                result.SuccessCount++;
            }

            await _context.SaveChangesAsync();
            result.Success = result.ErrorCount == 0;
            return result;
        }

        // ================= KHÓA KỲ CHẤM CÔNG =================
        public async Task<bool> IsPeriodLockedAsync(int year, int month, int? branchId = null, int? departmentId = null)
        {
            var query = _context.AttendanceLocks.AsNoTracking()
                .Where(l => l.Year == year && l.Month == month && l.IsLocked);

            if (branchId.HasValue && branchId > 0)
                query = query.Where(l => l.BranchId == null || l.BranchId == branchId.Value);

            if (departmentId.HasValue && departmentId > 0)
                query = query.Where(l => l.DepartmentId == null || l.DepartmentId == departmentId.Value);

            return await query.AnyAsync();
        }

        public async Task<List<AttendanceLockDto>> GetAttendanceLocksAsync(int year)
        {
            return await _context.AttendanceLocks
                .Include(l => l.Branch)
                .Include(l => l.Department)
                .Include(l => l.LockedBy)
                .Include(l => l.UnlockedBy)
                .Where(l => l.Year == year)
                .OrderByDescending(l => l.Month)
                .Select(l => new AttendanceLockDto
                {
                    Id = l.Id,
                    Month = l.Month,
                    Year = l.Year,
                    BranchId = l.BranchId,
                    BranchName = l.Branch != null ? l.Branch.Name : "Toàn công ty",
                    DepartmentId = l.DepartmentId,
                    DepartmentName = l.Department != null ? l.Department.Name : "Tất cả phòng ban",
                    IsLocked = l.IsLocked,
                    LockedAt = l.LockedAt,
                    LockedByName = l.LockedBy.Username,
                    Note = l.Note,
                    UnlockedAt = l.UnlockedAt,
                    UnlockedByName = l.UnlockedBy != null ? l.UnlockedBy.Username : null,
                    UnlockReason = l.UnlockReason
                })
                .ToListAsync();
        }

        public async Task<(bool Success, string? Error, int LockId)> LockPeriodAsync(
            int year, int month, int? branchId, int? departmentId, int lockedByUserId, string? note)
        {
            var existing = await _context.AttendanceLocks
                .FirstOrDefaultAsync(l => l.Year == year && l.Month == month &&
                                          l.BranchId == branchId && l.DepartmentId == departmentId);

            if (existing != null)
            {
                if (existing.IsLocked)
                    return (false, $"Kỳ chấm công tháng {month}/{year} đã được khóa trước đó.", existing.Id);

                existing.IsLocked = true;
                existing.LockedAt = VietnamTime.Now;
                existing.LockedById = lockedByUserId;
                existing.Note = note;
                existing.UnlockedAt = null;
                existing.UnlockedById = null;
                existing.UnlockReason = null;
                await _context.SaveChangesAsync();
                return (true, null, existing.Id);
            }

            var lockItem = new AttendanceLock
            {
                Year = year,
                Month = month,
                BranchId = branchId,
                DepartmentId = departmentId,
                IsLocked = true,
                LockedAt = VietnamTime.Now,
                LockedById = lockedByUserId,
                Note = note
            };

            _context.AttendanceLocks.Add(lockItem);
            await _context.SaveChangesAsync();
            return (true, null, lockItem.Id);
        }

        public async Task<(bool Success, string? Error)> UnlockPeriodAsync(
            int lockId, int unlockedByUserId, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return (false, "Bắt buộc phải nhập lý do mở khóa kỳ chấm công để ghi vết kiểm toán.");

            var lockItem = await _context.AttendanceLocks.FindAsync(lockId);
            if (lockItem == null) return (false, "Không tìm thấy thông tin khóa kỳ công.");

            if (!lockItem.IsLocked)
                return (false, "Kỳ công này hiện tại đang mở.");

            lockItem.IsLocked = false;
            lockItem.UnlockedAt = VietnamTime.Now;
            lockItem.UnlockedById = unlockedByUserId;
            lockItem.UnlockReason = reason;

            var user = await _context.Users.FindAsync(unlockedByUserId);

            // Ghi vết vào AuditLogs
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = unlockedByUserId,
                Username = user?.Username ?? "HR_Manager",
                Action = "UNLOCK_ATTENDANCE_PERIOD",
                EntityName = "AttendanceLock",
                RecordId = lockId.ToString(),
                NewValues = $"Mở khóa bảng công tháng {lockItem.Month}/{lockItem.Year}. Lý do: {reason}",
                CreatedAt = VietnamTime.Now
            });

            await _context.SaveChangesAsync();
            return (true, null);
        }
    }
}

