using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Filters;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.ViewModels;

namespace ERP_HRM_CMS.Controllers
{
    [Authorize]
    public class ChamCongController : Controller
    {
        private readonly AppDbContext _context;

        public ChamCongController(AppDbContext context)
        {
            _context = context;
        }

        // 1. BẢNG CHẤM CÔNG HÀNG NGÀY
        [HttpGet]
        [KiemTraQuyen("hrm.attendance.view")]
        public async Task<IActionResult> Index(DateTime? date, int? departmentId)
        {
            var selectedDate = date ?? DateTime.Today;

            var query = _context.AttendanceDailies
                .Include(a => a.Employee)
                    .ThenInclude(e => e.Department)
                .Include(a => a.Shift)
                .Where(a => a.WorkDate == selectedDate.Date)
                .AsQueryable();

            if (departmentId.HasValue && departmentId > 0)
                query = query.Where(a => a.Employee.DepartmentId == departmentId.Value);

            var attendances = await query.OrderBy(a => a.Employee.EmployeeCode).ToListAsync();

            // Nếu ngày chọn chưa có bản ghi nào, hiển thị danh sách nhân viên để điểm danh nhanh
            ViewBag.ActiveEmployees = await _context.Employees
                .Where(e => e.Status == "Active")
                .Include(e => e.Department)
                .OrderBy(e => e.FullName)
                .ToListAsync();

            ViewBag.Shifts = await _context.Shifts.Where(s => s.IsActive).ToListAsync();
            ViewBag.Departments = new SelectList(await _context.Departments.ToListAsync(), "Id", "Name", departmentId);

            var vm = new AttendanceDailyIndexViewModel
            {
                SelectedDate = selectedDate,
                DepartmentId = departmentId,
                AttendanceList = attendances,
                Shifts = await _context.Shifts.Where(s => s.IsActive).ToListAsync()
            };

            return View(vm);
        }

        // Điểm danh / Check-in / Check-out nhanh
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.attendance.manage")]
        public async Task<IActionResult> QuickAction(QuickCheckInDto dto)
        {
            var today = DateTime.Today;
            var shift = await _context.Shifts.FindAsync(dto.ShiftId);
            if (shift == null) return NotFound();

            var att = await _context.AttendanceDailies
                .FirstOrDefaultAsync(a => a.EmployeeId == dto.EmployeeId && a.WorkDate == today);

            var currentTime = DateTime.Now.TimeOfDay;

            if (att == null)
            {
                // Khởi tạo lượt vào (Check-in)
                int lateMin = 0;
                if (currentTime > shift.StartTime)
                {
                    lateMin = (int)(currentTime - shift.StartTime).TotalMinutes;
                }

                att = new AttendanceDaily
                {
                    EmployeeId = dto.EmployeeId,
                    ShiftId = dto.ShiftId,
                    WorkDate = today,
                    FirstInTime = currentTime,
                    LateMinutes = lateMin,
                    AttendanceStatus = (lateMin > 0) ? "Late" : "Present",
                    CreatedAt = DateTime.Now
                };
                _context.AttendanceDailies.Add(att);
            }
            else
            {
                // Ghi nhận lượt ra (Check-out)
                att.LastOutTime = currentTime;

                int earlyMin = 0;
                if (currentTime < shift.EndTime)
                {
                    earlyMin = (int)(shift.EndTime - currentTime).TotalMinutes;
                }
                att.EarlyMinutes = earlyMin;

                // Tính toán số giờ làm việc
                if (att.FirstInTime.HasValue)
                {
                    var totalTime = (currentTime - att.FirstInTime.Value).TotalHours;
                    att.WorkHourCount = Math.Min((decimal)totalTime, shift.WorkHours);

                    if (currentTime > shift.EndTime)
                    {
                        att.OvertimeHours = (decimal)(currentTime - shift.EndTime).TotalHours;
                    }
                }
                att.AttendanceStatus = (att.LateMinutes > 0) ? "Late" : "Present";
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Ghi nhận thông tin chấm công thành công!";
            return RedirectToAction(nameof(Index), new { date = today });
        }

        // 2. BẢNG CÔNG TỔNG HỢP THEO THÁNG
        [HttpGet]
        [KiemTraQuyen("hrm.attendance.view")]
        public async Task<IActionResult> BangCongThang(int? year, int? month, int? departmentId)
        {
            int targetYear = year ?? DateTime.Today.Year;
            int targetMonth = month ?? DateTime.Today.Month;
            int daysInMonth = DateTime.DaysInMonth(targetYear, targetMonth);

            var startDate = new DateTime(targetYear, targetMonth, 1);
            var endDate = new DateTime(targetYear, targetMonth, daysInMonth);

            var empQuery = _context.Employees
                .Include(e => e.Department)
                .Where(e => e.Status == "Active")
                .AsQueryable();

            if (departmentId.HasValue && departmentId > 0)
                empQuery = empQuery.Where(e => e.DepartmentId == departmentId.Value);

            var employees = await empQuery.OrderBy(e => e.EmployeeCode).ToListAsync();

            // Lấy toàn bộ bản ghi công trong tháng
            var attendances = await _context.AttendanceDailies
                .Where(a => a.WorkDate >= startDate && a.WorkDate <= endDate)
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

                for (int day = 1; day <= daysInMonth; day++)
                {
                    var record = empAtts.FirstOrDefault(a => a.WorkDate.Day == day);
                    if (record != null)
                    {
                        string badge = record.AttendanceStatus switch
                        {
                            "Present" => "P",
                            "Late" => "L",
                            "Early" => "E",
                            "Absent" => "A",
                            _ => "P"
                        };
                        row.DailyStatuses[day] = badge;
                        row.TotalWorkHours += record.WorkHourCount;
                        row.TotalLateMinutes += record.LateMinutes;
                        if (record.AttendanceStatus != "Absent") row.PresentDaysCount++;
                    }
                    else
                    {
                        // Kiểm tra ngày cuối tuần (Thứ 7, CN)
                        var curDate = new DateTime(targetYear, targetMonth, day);
                        row.DailyStatuses[day] = (curDate.DayOfWeek == DayOfWeek.Sunday || curDate.DayOfWeek == DayOfWeek.Saturday) ? "-" : "x";
                    }
                }

                rows.Add(row);
            }

            ViewBag.Departments = new SelectList(await _context.Departments.ToListAsync(), "Id", "Name", departmentId);

            var vm = new MonthlyTimesheetViewModel
            {
                Year = targetYear,
                Month = targetMonth,
                DepartmentId = departmentId,
                DaysInMonth = daysInMonth,
                Rows = rows
            };

            return View(vm);
        }

        // 3. QUẢN LÝ ĐỊNH NGHĨA CA LÀM VIỆC (SHIFTS)
        [HttpGet]
        [KiemTraQuyen("hrm.attendance.view")]
        public async Task<IActionResult> CaLamViec()
        {
            var shifts = await _context.Shifts.OrderBy(s => s.StartTime).ToListAsync();
            return View(shifts);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.attendance.manage")]
        public async Task<IActionResult> TaoCa(Shift shift)
        {
            if (ModelState.IsValid)
            {
                _context.Shifts.Add(shift);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Tạo ca làm việc '{shift.Name}' thành công!";
            }
            return RedirectToAction(nameof(CaLamViec));
        }

        // 4. BIỂU ĐỒ HEATMAP CHẤM CÔNG THEO LƯỚI LỊCH THÁNG (CALENDAR GRID HEATMAP)
        [HttpGet]
        [KiemTraQuyen("hrm.attendance.view")]
        public async Task<IActionResult> Heatmap(int? year, int? month, int? departmentId)
        {
            int targetYear = year ?? DateTime.Today.Year;
            int targetMonth = month ?? DateTime.Today.Month;
            int daysInMonth = DateTime.DaysInMonth(targetYear, targetMonth);

            var startDate = new DateTime(targetYear, targetMonth, 1);
            var endDate = new DateTime(targetYear, targetMonth, daysInMonth);

            var empQuery = _context.Employees
                .Include(e => e.Department)
                .Where(e => e.Status == "Active")
                .AsQueryable();

            if (departmentId.HasValue && departmentId > 0)
                empQuery = empQuery.Where(e => e.DepartmentId == departmentId.Value);

            var employees = await empQuery.OrderBy(e => e.EmployeeCode).ToListAsync();

            var attendances = await _context.AttendanceDailies
                .Where(a => a.WorkDate >= startDate && a.WorkDate <= endDate)
                .ToListAsync();

            var approvedLeaves = await _context.LeaveRequests
                .Where(l => l.Status == "GIAM_DOC_DUYET" &&
                           ((l.FromDate >= startDate && l.FromDate <= endDate) ||
                            (l.ToDate >= startDate && l.ToDate <= endDate)))
                .ToListAsync();

            var vm = new AttendanceHeatmapViewModel
            {
                Year = targetYear,
                Month = targetMonth,
                DepartmentId = departmentId,
                DaysInMonth = daysInMonth
            };

            foreach (var emp in employees)
            {
                var row = new EmployeeHeatmapRowDto
                {
                    EmployeeId = emp.Id,
                    EmployeeCode = emp.EmployeeCode,
                    FullName = emp.FullName,
                    DepartmentName = emp.Department?.Name ?? "Chưa gán"
                };

                var empAtts = attendances.Where(a => a.EmployeeId == emp.Id).ToList();
                var empLeaves = approvedLeaves.Where(l => l.EmployeeId == emp.Id).ToList();

                for (int day = 1; day <= daysInMonth; day++)
                {
                    var currentDate = new DateTime(targetYear, targetMonth, day);
                    var att = empAtts.FirstOrDefault(a => a.WorkDate.Day == day);
                    var leave = empLeaves.FirstOrDefault(l => currentDate >= l.FromDate.Date && currentDate <= l.ToDate.Date);

                    var cell = new HeatmapCellDto { Day = day };

                    if (att != null)
                    {
                        cell.WorkHours = att.WorkHourCount;
                        cell.OvertimeHours = att.OvertimeHours;

                        if (att.AttendanceStatus == "Late" || att.LateMinutes > 0)
                        {
                            cell.Status = "Late";
                            cell.ColorClass = "heatmap-late";
                            cell.IconClass = "bi-clock-fill";
                            cell.Tooltip = $"Ngày {day:D2}/{targetMonth:D2}: Vào trễ {att.LateMinutes}p ({att.FirstInTime:hh\\:mm} - {att.LastOutTime:hh\\:mm})";
                            row.LateCount++;
                            vm.TotalLateDays++;
                        }
                        else if (att.AttendanceStatus == "Early" || att.EarlyMinutes > 0)
                        {
                            cell.Status = "Early";
                            cell.ColorClass = "heatmap-late";
                            cell.IconClass = "bi-arrow-left-circle";
                            cell.Tooltip = $"Ngày {day:D2}/{targetMonth:D2}: Về sớm {att.EarlyMinutes}p ({att.FirstInTime:hh\\:mm} - {att.LastOutTime:hh\\:mm})";
                            row.LateCount++;
                            vm.TotalLateDays++;
                        }
                        else if (att.AttendanceStatus == "Leave")
                        {
                            cell.Status = "Leave";
                            cell.ColorClass = "heatmap-leave";
                            cell.IconClass = "bi-file-earmark-medical";
                            cell.Tooltip = $"Ngày {day:D2}/{targetMonth:D2}: Nghỉ phép có duyệt ({att.Note})";
                            row.LeaveCount++;
                            vm.TotalLeaveDays++;
                        }
                        else if (att.AttendanceStatus == "BusinessTrip")
                        {
                            cell.Status = "BusinessTrip";
                            cell.ColorClass = "heatmap-trip";
                            cell.IconClass = "bi-briefcase";
                            cell.Tooltip = $"Ngày {day:D2}/{targetMonth:D2}: Đi công tác ({att.Note})";
                            row.PresentCount++;
                            vm.TotalPresentDays++;
                        }
                        else if (att.AttendanceStatus == "Absent")
                        {
                            cell.Status = "Absent";
                            cell.ColorClass = "heatmap-absent";
                            cell.IconClass = "bi-x-circle-fill";
                            cell.Tooltip = $"Ngày {day:D2}/{targetMonth:D2}: Vắng mặt không phép";
                            row.AbsentCount++;
                            vm.TotalAbsentDays++;
                        }
                        else // Present
                        {
                            cell.Status = "Present";
                            cell.ColorClass = "heatmap-present";
                            cell.IconClass = "bi-check-circle-fill";
                            cell.Tooltip = $"Ngày {day:D2}/{targetMonth:D2}: Chuẩn giờ ({att.FirstInTime:hh\\:mm} - {att.LastOutTime:hh\\:mm}) - Làm {att.WorkHourCount}h" + (att.OvertimeHours > 0 ? $" [OT {att.OvertimeHours}h]" : "");
                            row.PresentCount++;
                            vm.TotalPresentDays++;
                        }

                        row.TotalOT += att.OvertimeHours;
                    }
                    else if (leave != null)
                    {
                        cell.Status = "Leave";
                        cell.ColorClass = "heatmap-leave";
                        cell.IconClass = "bi-file-earmark-medical";
                        cell.Tooltip = $"Ngày {day:D2}/{targetMonth:D2}: Nghỉ phép có duyệt ({leave.RequestCode})";
                        row.LeaveCount++;
                        vm.TotalLeaveDays++;
                    }
                    else if (currentDate.DayOfWeek == DayOfWeek.Sunday || currentDate.DayOfWeek == DayOfWeek.Saturday)
                    {
                        cell.Status = "Off";
                        cell.ColorClass = "heatmap-off";
                        cell.IconClass = "bi-dash";
                        cell.Tooltip = $"Ngày {day:D2}/{targetMonth:D2}: Nghỉ cuối tuần ({(currentDate.DayOfWeek == DayOfWeek.Sunday ? "Chủ Nhật" : "Thứ 7")})";
                    }
                    else if (currentDate <= DateTime.Today)
                    {
                        cell.Status = "Absent";
                        cell.ColorClass = "heatmap-absent";
                        cell.IconClass = "bi-exclamation-circle";
                        cell.Tooltip = $"Ngày {day:D2}/{targetMonth:D2}: Không chấm công (Vắng)";
                        row.AbsentCount++;
                        vm.TotalAbsentDays++;
                    }
                    else
                    {
                        cell.Status = "Future";
                        cell.ColorClass = "heatmap-future";
                        cell.IconClass = "";
                        cell.Tooltip = $"Ngày {day:D2}/{targetMonth:D2}: Chưa tới ngày";
                    }

                    row.DayCells[day] = cell;
                }

                vm.EmployeeRows.Add(row);
            }

            int totalLoggedWorkDays = vm.TotalPresentDays + vm.TotalLateDays + vm.TotalAbsentDays;
            vm.OnTimePercentage = totalLoggedWorkDays > 0 
                ? Math.Round((decimal)vm.TotalPresentDays / totalLoggedWorkDays * 100m, 1) 
                : 100m;

            ViewBag.Departments = new SelectList(await _context.Departments.OrderBy(d => d.Name).ToListAsync(), "Id", "Name", departmentId);
            return View(vm);
        }
    }
}
