using ERP.Application.Common.Interfaces;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ERP_HRM_CMS.Services.HRM
{
    public class ShiftScheduleService : IShiftScheduleService
    {
        private readonly AppDbContext _context;
        private readonly IDataScopeService? _dataScope;

        public ShiftScheduleService(AppDbContext context, IDataScopeService? dataScope = null)
        {
            _context = context;
            _dataScope = dataScope;
        }

        public async Task<List<Shift>> GetAllShiftsAsync()
        {
            return await _context.Shifts
                .AsNoTracking()
                .OrderBy(s => s.StartTime)
                .ToListAsync();
        }

        public async Task<Shift?> GetShiftByIdAsync(int id)
        {
            return await _context.Shifts
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<(bool Success, string? Error, int Id)> CreateShiftAsync(Shift shift, int? currentUserId)
        {
            if (await _context.Shifts.AnyAsync(s => s.Code == shift.Code))
                return (false, "Mã ca làm việc đã tồn tại trong hệ thống.", 0);

            if (shift.WorkHours <= 0)
            {
                if (shift.EndTime > shift.StartTime)
                    shift.WorkHours = (decimal)(shift.EndTime - shift.StartTime).TotalHours;
                else
                    shift.WorkHours = (decimal)(TimeSpan.FromHours(24) - shift.StartTime + shift.EndTime).TotalHours;
            }

            _context.Shifts.Add(shift);
            await _context.SaveChangesAsync();
            return (true, null, shift.Id);
        }

        public async Task<(bool Success, string? Error)> UpdateShiftAsync(int id, Shift shift, int? currentUserId)
        {
            var existing = await _context.Shifts.FindAsync(id);
            if (existing == null) return (false, "Không tìm thấy ca làm việc.");

            if (await _context.Shifts.AnyAsync(s => s.Code == shift.Code && s.Id != id))
                return (false, "Mã ca làm việc đã bị trùng với ca khác.");

            existing.Code = shift.Code;
            existing.Name = shift.Name;
            existing.StartTime = shift.StartTime;
            existing.EndTime = shift.EndTime;
            existing.BreakStartTime = shift.BreakStartTime;
            existing.BreakEndTime = shift.BreakEndTime;
            existing.WorkHours = shift.WorkHours;
            existing.IsActive = shift.IsActive;

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> DeleteShiftAsync(int id)
        {
            var existing = await _context.Shifts.FindAsync(id);
            if (existing == null) return (false, "Không tìm thấy ca làm việc.");

            bool hasAttendances = await _context.AttendanceDailies.AnyAsync(a => a.ShiftId == id);
            if (hasAttendances)
            {
                existing.IsActive = false; // Soft deactivate
                await _context.SaveChangesAsync();
                return (true, "Ca làm việc đã phát sinh dữ liệu chấm công, hệ thống đã tự động chuyển sang trạng thái Ngừng kích hoạt.");
            }

            _context.Shifts.Remove(existing);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<WeeklyRosterDto> GetWeeklyRosterAsync(DateTime weekStartDate, int? departmentId, int? branchId)
        {
            // Chuẩn hóa về Thứ 2 đầu tuần
            int diff = (7 + (weekStartDate.DayOfWeek - DayOfWeek.Monday)) % 7;
            var monday = weekStartDate.Date.AddDays(-1 * diff);
            var sunday = monday.AddDays(6);

            var daysOfWeek = new List<DateTime>();
            for (int i = 0; i < 7; i++)
            {
                daysOfWeek.Add(monday.AddDays(i));
            }

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
            var availableShifts = await _context.Shifts.Where(s => s.IsActive).OrderBy(s => s.StartTime).ToListAsync();
            var defaultShift = availableShifts.FirstOrDefault();

            var attendances = await _context.AttendanceDailies
                .Include(a => a.Shift)
                .Where(a => a.WorkDate >= monday && a.WorkDate <= sunday)
                .AsNoTracking()
                .ToListAsync();

            var employeeRows = new List<RosterEmployeeRowDto>();

            foreach (var emp in employees)
            {
                var row = new RosterEmployeeRowDto
                {
                    EmployeeId = emp.Id,
                    EmployeeCode = emp.EmployeeCode,
                    FullName = emp.FullName,
                    DepartmentName = emp.Department?.Name ?? "Chưa gán"
                };

                for (int i = 0; i < 7; i++)
                {
                    var currentDate = daysOfWeek[i];
                    var record = attendances.FirstOrDefault(a => a.EmployeeId == emp.Id && a.WorkDate.Date == currentDate.Date);
                    var shift = record?.Shift ?? defaultShift;

                    bool isWeekend = currentDate.DayOfWeek == DayOfWeek.Saturday || currentDate.DayOfWeek == DayOfWeek.Sunday;

                    string colorClass = "shift-default";
                    if (shift != null)
                    {
                        if (shift.IsOvernight) colorClass = "shift-night";
                        else if (shift.ShiftType == "Split") colorClass = "shift-split";
                        else colorClass = "shift-office";
                    }

                    row.DailyShifts[i + 1] = new RosterCellDto
                    {
                        Date = currentDate,
                        DayIndex = i + 1,
                        ShiftId = shift?.Id,
                        ShiftCode = shift?.Code ?? "OFF",
                        ShiftName = shift?.Name ?? (isWeekend ? "Nghỉ tuần" : "Chưa gán"),
                        ShiftTime = shift != null ? $"{shift.StartTime:hh\\:mm} - {shift.EndTime:hh\\:mm}" : "—",
                        IsOvernight = shift?.IsOvernight ?? false,
                        IsWeekend = isWeekend,
                        ColorClass = colorClass
                    };
                }

                employeeRows.Add(row);
            }

            return new WeeklyRosterDto
            {
                WeekStartDate = monday,
                WeekEndDate = sunday,
                DepartmentId = departmentId,
                BranchId = branchId,
                DaysOfWeek = daysOfWeek,
                EmployeeRows = employeeRows,
                AvailableShifts = availableShifts
            };
        }

        public async Task<(bool Success, string? Error)> AssignShiftAsync(int employeeId, DateTime date, int shiftId, int? currentUserId)
        {
            // Kiểm tra khóa kỳ công
            bool isLocked = await _context.AttendanceLocks.AnyAsync(l =>
                l.Year == date.Year && l.Month == date.Month && l.IsLocked);

            if (isLocked)
                return (false, $"Kỳ chấm công tháng {date.Month}/{date.Year} đã bị khóa. Không thể điều chỉnh phân ca.");

            var shift = await _context.Shifts.FindAsync(shiftId);
            if (shift == null) return (false, "Ca làm việc không hợp lệ.");

            var att = await _context.AttendanceDailies
                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.WorkDate.Date == date.Date);

            if (att == null)
            {
                att = new AttendanceDaily
                {
                    EmployeeId = employeeId,
                    ShiftId = shiftId,
                    WorkDate = date.Date,
                    AttendanceStatus = "Scheduled",
                    CreatedAt = VietnamTime.Now
                };
                _context.AttendanceDailies.Add(att);
            }
            else
            {
                att.ShiftId = shiftId;
            }

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error, int Count)> BatchAssignShiftAsync(
            List<int> employeeIds, DateTime fromDate, DateTime toDate, int shiftId, bool skipWeekends, int? currentUserId)
        {
            if (toDate < fromDate)
                return (false, "Ngày kết thúc không được nhỏ hơn ngày bắt đầu.", 0);

            var shift = await _context.Shifts.FindAsync(shiftId);
            if (shift == null) return (false, "Ca làm việc không hợp lệ.", 0);

            // Kiểm tra khóa kỳ
            for (var m = new DateTime(fromDate.Year, fromDate.Month, 1); m <= toDate; m = m.AddMonths(1))
            {
                bool isLocked = await _context.AttendanceLocks.AnyAsync(l =>
                    l.Year == m.Year && l.Month == m.Month && l.IsLocked);
                if (isLocked)
                    return (false, $"Kỳ chấm công tháng {m.Month}/{m.Year} đang bị khóa.", 0);
            }

            int count = 0;
            var allAttendances = await _context.AttendanceDailies
                .Where(a => employeeIds.Contains(a.EmployeeId) && a.WorkDate >= fromDate.Date && a.WorkDate <= toDate.Date)
                .ToListAsync();

            for (var d = fromDate.Date; d <= toDate.Date; d = d.AddDays(1))
            {
                if (skipWeekends && (d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday))
                    continue;

                foreach (var empId in employeeIds)
                {
                    var att = allAttendances.FirstOrDefault(a => a.EmployeeId == empId && a.WorkDate.Date == d);
                    if (att == null)
                    {
                        var newAtt = new AttendanceDaily
                        {
                            EmployeeId = empId,
                            ShiftId = shiftId,
                            WorkDate = d,
                            AttendanceStatus = "Scheduled",
                            CreatedAt = VietnamTime.Now
                        };
                        _context.AttendanceDailies.Add(newAtt);
                        allAttendances.Add(newAtt);
                    }
                    else
                    {
                        att.ShiftId = shiftId;
                    }
                    count++;
                }
            }

            await _context.SaveChangesAsync();
            return (true, null, count);
        }

        public async Task<(bool Success, string? Error, int CopiedCount)> CopyWeekScheduleAsync(
            DateTime sourceWeekStart, DateTime targetWeekStart, int? departmentId, int? branchId, int? currentUserId)
        {
            int sourceDiff = (7 + (sourceWeekStart.DayOfWeek - DayOfWeek.Monday)) % 7;
            var srcMonday = sourceWeekStart.Date.AddDays(-1 * sourceDiff);
            var srcSunday = srcMonday.AddDays(6);

            int targetDiff = (7 + (targetWeekStart.DayOfWeek - DayOfWeek.Monday)) % 7;
            var tgtMonday = targetWeekStart.Date.AddDays(-1 * targetDiff);
            var tgtSunday = tgtMonday.AddDays(6);

            // Kiểm tra khóa kỳ tuần đích
            for (var d = tgtMonday; d <= tgtSunday; d = d.AddDays(1))
            {
                bool isLocked = await _context.AttendanceLocks.AnyAsync(l =>
                    l.Year == d.Year && l.Month == d.Month && l.IsLocked);
                if (isLocked)
                    return (false, $"Kỳ chấm công tháng {d.Month}/{d.Year} của tuần đích đang bị khóa.", 0);
            }

            var empQuery = _context.Employees
                .Where(e => e.Status == "Active")
                .AsQueryable();

            if (departmentId.HasValue && departmentId > 0)
                empQuery = empQuery.Where(e => e.DepartmentId == departmentId.Value);

            var empIds = await empQuery.Select(e => e.Id).ToListAsync();

            var sourceRecords = await _context.AttendanceDailies
                .Where(a => empIds.Contains(a.EmployeeId) && a.WorkDate >= srcMonday && a.WorkDate <= srcSunday)
                .AsNoTracking()
                .ToListAsync();

            if (!sourceRecords.Any())
                return (false, "Tuần nguồn chưa có lịch phân ca nào để sao chép.", 0);

            var targetRecords = await _context.AttendanceDailies
                .Where(a => empIds.Contains(a.EmployeeId) && a.WorkDate >= tgtMonday && a.WorkDate <= tgtSunday)
                .ToListAsync();

            int copied = 0;
            foreach (var src in sourceRecords)
            {
                int dayOffset = (src.WorkDate.Date - srcMonday).Days;
                var targetDate = tgtMonday.AddDays(dayOffset);

                var tgt = targetRecords.FirstOrDefault(a => a.EmployeeId == src.EmployeeId && a.WorkDate.Date == targetDate);
                if (tgt == null)
                {
                    _context.AttendanceDailies.Add(new AttendanceDaily
                    {
                        EmployeeId = src.EmployeeId,
                        ShiftId = src.ShiftId,
                        WorkDate = targetDate,
                        AttendanceStatus = "Scheduled",
                        CreatedAt = VietnamTime.Now
                    });
                }
                else
                {
                    tgt.ShiftId = src.ShiftId;
                }
                copied++;
            }

            await _context.SaveChangesAsync();
            return (true, null, copied);
        }

        public async Task<List<SelectListItem>> GetShiftSelectListAsync(int? selectedId = null)
        {
            return await _context.Shifts
                .Where(s => s.IsActive)
                .OrderBy(s => s.StartTime)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = $"{s.Code} - {s.Name} ({s.StartTime:hh\\:mm} - {s.EndTime:hh\\:mm})",
                    Selected = selectedId.HasValue && s.Id == selectedId.Value
                })
                .ToListAsync();
        }
    }
}

