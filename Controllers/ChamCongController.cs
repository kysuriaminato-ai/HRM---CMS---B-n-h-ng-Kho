using ERP.Application.Common.Interfaces;
using ERP_HRM_CMS.Filters;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.ViewModels;
using ERP_HRM_CMS.Services.HRM;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ERP_HRM_CMS.Controllers
{
    [Authorize]
    public class ChamCongController : Controller
    {
        private readonly IAttendanceService _attendanceService;
        private readonly IShiftScheduleService _shiftService;
        private readonly IDepartmentService _departmentService;
        private readonly ICurrentUserService _currentUser;

        public ChamCongController(
            IAttendanceService attendanceService,
            IShiftScheduleService shiftService,
            IDepartmentService departmentService,
            ICurrentUserService currentUser)
        {
            _attendanceService = attendanceService;
            _shiftService = shiftService;
            _departmentService = departmentService;
            _currentUser = currentUser;
        }

        private int? CurrentUserId => _currentUser.UserId;

        // ═══════════════════════════════════════════════════════════
        // 1. BẢNG CHẤM CÔNG HÀNG NGÀY
        // ═══════════════════════════════════════════════════════════
        [HttpGet]
        [KiemTraQuyen("hrm.attendance.view")]
        public async Task<IActionResult> Index(
            DateTime? date, int? departmentId, int? branchId, string? status, string? keyword, int page = 1)
        {
            var selectedDate = date ?? VietnamTime.Today;

            var filter = new AttendanceDailyFilterDto
            {
                SelectedDate = selectedDate,
                DepartmentId = departmentId,
                BranchId = branchId,
                Status = status,
                Keyword = keyword,
                Page = page,
                PageSize = 30
            };

            var paged = await _attendanceService.GetDailyAttendancePagedAsync(filter);
            ViewBag.Filter = filter;
            ViewBag.Departments = await _departmentService.GetDepartmentSelectListAsync(departmentId);
            ViewBag.Shifts = await _shiftService.GetAllShiftsAsync();
            ViewBag.IsPeriodLocked = await _attendanceService.IsPeriodLockedAsync(selectedDate.Year, selectedDate.Month, branchId, departmentId);

            return View(paged);
        }

        // Điểm danh / Check-in / Check-out nhanh
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.attendance.manage")]
        public async Task<IActionResult> QuickAction(QuickCheckInDto dto)
        {
            var (success, error) = await _attendanceService.QuickCheckInAsync(dto, CurrentUserId);
            if (success)
            {
                TempData["SuccessMessage"] = "Ghi nhận thông tin chấm công thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = error;
            }

            return RedirectToAction(nameof(Index), new { date = VietnamTime.Today });
        }

        // ═══════════════════════════════════════════════════════════
        // 2. BẢNG CÔNG TỔNG HỢP THEO THÁNG (COMPACT MATRIX)
        // ═══════════════════════════════════════════════════════════
        [HttpGet]
        [KiemTraQuyen("hrm.attendance.view")]
        public async Task<IActionResult> BangCongThang(int? year, int? month, int? departmentId, int? branchId)
        {
            int targetYear = year ?? VietnamTime.Today.Year;
            int targetMonth = month ?? VietnamTime.Today.Month;

            var vm = await _attendanceService.GetMonthlyTimesheetAsync(targetYear, targetMonth, departmentId, branchId);
            ViewBag.Departments = await _departmentService.GetDepartmentSelectListAsync(departmentId);
            ViewBag.IsPeriodLocked = await _attendanceService.IsPeriodLockedAsync(targetYear, targetMonth, branchId, departmentId);
            ViewBag.Locks = await _attendanceService.GetAttendanceLocksAsync(targetYear);

            return View(vm);
        }

        // Drawer AJAX xem chi tiết ngày công
        [HttpGet]
        [KiemTraQuyen("hrm.attendance.view")]
        public async Task<IActionResult> GetDayDetail(int employeeId, DateTime date)
        {
            var detail = await _attendanceService.GetDayAttendanceDetailAsync(employeeId, date);
            if (detail == null) return NotFound(new { success = false, message = "Không tìm thấy dữ liệu." });
            return Json(new { success = true, data = detail });
        }

        // Xuất bảng công Excel
        [HttpGet]
        [KiemTraQuyen("hrm.attendance.view")]
        public async Task<IActionResult> ExportExcel(int? year, int? month, int? departmentId, int? branchId)
        {
            int targetYear = year ?? VietnamTime.Today.Year;
            int targetMonth = month ?? VietnamTime.Today.Month;

            var bytes = await _attendanceService.ExportMonthlyTimesheetToExcelAsync(targetYear, targetMonth, departmentId, branchId);
            string fileName = $"BangCong_T{targetMonth}_{targetYear}_{VietnamTime.Now:yyyyMMddHHmmss}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // ═══════════════════════════════════════════════════════════
        // 3. IMPORT DỮ LIỆU MÁY CHẤM CÔNG (EXCEL / CSV)
        // ═══════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.attendance.manage")]
        public async Task<IActionResult> ImportExcel(IFormFile? file, int? defaultShiftId)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn file Excel hoặc CSV chấm công hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            using var stream = file.OpenReadStream();
            var result = await _attendanceService.ImportAttendanceLogAsync(stream, file.FileName, defaultShiftId, CurrentUserId);

            if (result.Success)
            {
                TempData["SuccessMessage"] = $"Import thành công {result.SuccessCount}/{result.TotalRows} dòng dữ liệu chấm công!";
            }
            else
            {
                TempData["ErrorMessage"] = $"Import hoàn tất: {result.SuccessCount} thành công, {result.ErrorCount} lỗi. " +
                                          string.Join(" | ", result.ErrorMessages.Take(3));
            }

            return RedirectToAction(nameof(Index));
        }

        // ═══════════════════════════════════════════════════════════
        // 4. PHÂN CA LÀM VIỆC (SHIFT ROSTER)
        // ═══════════════════════════════════════════════════════════
        [HttpGet]
        [KiemTraQuyen("hrm.attendance.view")]
        public async Task<IActionResult> PhanCa(DateTime? weekStart, int? departmentId, int? branchId)
        {
            var targetWeek = weekStart ?? VietnamTime.Today;
            var roster = await _shiftService.GetWeeklyRosterAsync(targetWeek, departmentId, branchId);
            ViewBag.Departments = await _departmentService.GetDepartmentSelectListAsync(departmentId);
            ViewBag.ShiftSelectList = await _shiftService.GetShiftSelectListAsync();
            return View(roster);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.attendance.manage")]
        public async Task<IActionResult> AssignShift(int employeeId, DateTime date, int shiftId)
        {
            var (success, error) = await _shiftService.AssignShiftAsync(employeeId, date, shiftId, CurrentUserId);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success, error });

            if (success)
                TempData["SuccessMessage"] = "Gán ca làm việc thành công!";
            else
                TempData["ErrorMessage"] = error;

            return RedirectToAction(nameof(PhanCa), new { weekStart = date });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.attendance.manage")]
        public async Task<IActionResult> CopySchedule(DateTime sourceWeek, DateTime targetWeek, int? departmentId, int? branchId)
        {
            var (success, error, count) = await _shiftService.CopyWeekScheduleAsync(
                sourceWeek, targetWeek, departmentId, branchId, CurrentUserId);

            if (success)
            {
                TempData["SuccessMessage"] = $"Sao chép thành công {count} lượt phân ca từ tuần {sourceWeek:dd/MM} sang tuần {targetWeek:dd/MM}!";
            }
            else
            {
                TempData["ErrorMessage"] = error;
            }

            return RedirectToAction(nameof(PhanCa), new { weekStart = targetWeek, departmentId, branchId });
        }

        // ═══════════════════════════════════════════════════════════
        // 5. KHÓA & MỞ KHÓA KỲ CHẤM CÔNG (ATTENDANCE LOCK)
        // ═══════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.attendance.manage")]
        public async Task<IActionResult> KhoaKyCong(int year, int month, int? branchId, int? departmentId, string? note)
        {
            var (success, error, _) = await _attendanceService.LockPeriodAsync(
                year, month, branchId, departmentId, CurrentUserId ?? 1, note);

            if (success)
                TempData["SuccessMessage"] = $"Đã khóa thành công bảng công tháng {month}/{year}!";
            else
                TempData["ErrorMessage"] = error;

            return RedirectToAction(nameof(BangCongThang), new { year, month, departmentId, branchId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.attendance.manage")]
        public async Task<IActionResult> MoKhoaKyCong(int lockId, string reason, int year, int month)
        {
            var (success, error) = await _attendanceService.UnlockPeriodAsync(
                lockId, CurrentUserId ?? 1, reason);

            if (success)
                TempData["SuccessMessage"] = $"Đã mở khóa kỳ công tháng {month}/{year} và lưu vết kiểm toán!";
            else
                TempData["ErrorMessage"] = error;

            return RedirectToAction(nameof(BangCongThang), new { year, month });
        }

        // ═══════════════════════════════════════════════════════════
        // 6. ĐỊNH NGHĨA CA LÀM VIỆC (SHIFTS)
        // ═══════════════════════════════════════════════════════════
        [HttpGet]
        [KiemTraQuyen("hrm.attendance.view")]
        public async Task<IActionResult> CaLamViec()
        {
            var shifts = await _shiftService.GetAllShiftsAsync();
            return View(shifts);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.attendance.manage")]
        public async Task<IActionResult> TaoCa(Shift shift)
        {
            if (ModelState.IsValid)
            {
                var (success, error, _) = await _shiftService.CreateShiftAsync(shift, CurrentUserId);
                if (success)
                {
                    TempData["SuccessMessage"] = $"Tạo ca làm việc '{shift.Name}' thành công!";
                    return RedirectToAction(nameof(CaLamViec));
                }
                ModelState.AddModelError("", error ?? "Không thể tạo ca.");
            }
            var shifts = await _shiftService.GetAllShiftsAsync();
            return View(nameof(CaLamViec), shifts);
        }

        // ═══════════════════════════════════════════════════════════
        // 7. HEATMAP CHẤM CÔNG THEO THÁNG
        // ═══════════════════════════════════════════════════════════
        [HttpGet]
        [KiemTraQuyen("hrm.attendance.view")]
        public async Task<IActionResult> Heatmap(int? year, int? month, int? departmentId)
        {
            int targetYear = year ?? VietnamTime.Today.Year;
            int targetMonth = month ?? VietnamTime.Today.Month;
            int daysInMonth = DateTime.DaysInMonth(targetYear, targetMonth);

            var timesheet = await _attendanceService.GetMonthlyTimesheetAsync(targetYear, targetMonth, departmentId, null);

            var vm = new AttendanceHeatmapViewModel
            {
                Year = targetYear,
                Month = targetMonth,
                DepartmentId = departmentId,
                DaysInMonth = daysInMonth
            };

            foreach (var r in timesheet.Rows)
            {
                var row = new EmployeeHeatmapRowDto
                {
                    EmployeeId = r.EmployeeId,
                    EmployeeCode = r.EmployeeCode,
                    FullName = r.FullName,
                    DepartmentName = r.DepartmentName
                };

                for (int day = 1; day <= daysInMonth; day++)
                {
                    var curDate = new DateTime(targetYear, targetMonth, day);
                    string status = r.DailyStatuses.ContainsKey(day) ? r.DailyStatuses[day] : "-";

                    var cell = new HeatmapCellDto { Day = day };
                    if (status.StartsWith("X"))
                    {
                        cell.Status = "Present";
                        cell.ColorClass = "heatmap-present";
                        cell.IconClass = "bi-check-circle-fill";
                        cell.Tooltip = $"Ngày {day:D2}/{targetMonth:D2}: Đủ công chuẩn";
                        row.PresentCount++;
                        vm.TotalPresentDays++;
                    }
                    else if (status.StartsWith("M"))
                    {
                        cell.Status = "Late";
                        cell.ColorClass = "heatmap-late";
                        cell.IconClass = "bi-clock-fill";
                        cell.Tooltip = $"Ngày {day:D2}/{targetMonth:D2}: Đi muộn/về sớm";
                        row.LateCount++;
                        vm.TotalLateDays++;
                    }
                    else if (status.StartsWith("P"))
                    {
                        cell.Status = "Leave";
                        cell.ColorClass = "heatmap-leave";
                        cell.IconClass = "bi-file-earmark-medical";
                        cell.Tooltip = $"Ngày {day:D2}/{targetMonth:D2}: Nghỉ phép có duyệt";
                        row.LeaveCount++;
                        vm.TotalLeaveDays++;
                    }
                    else if (status == "-")
                    {
                        cell.Status = "Off";
                        cell.ColorClass = "heatmap-off";
                        cell.IconClass = "bi-dash";
                        cell.Tooltip = $"Ngày {day:D2}/{targetMonth:D2}: Cuối tuần";
                    }
                    else
                    {
                        cell.Status = "Absent";
                        cell.ColorClass = "heatmap-absent";
                        cell.IconClass = "bi-exclamation-circle";
                        cell.Tooltip = $"Ngày {day:D2}/{targetMonth:D2}: Không có công";
                        row.AbsentCount++;
                        vm.TotalAbsentDays++;
                    }

                    row.DayCells[day] = cell;
                }

                vm.EmployeeRows.Add(row);
            }

            int totalLoggedWorkDays = vm.TotalPresentDays + vm.TotalLateDays + vm.TotalAbsentDays;
            vm.OnTimePercentage = totalLoggedWorkDays > 0
                ? Math.Round((decimal)vm.TotalPresentDays / totalLoggedWorkDays * 100m, 1)
                : 100m;

            ViewBag.Departments = await _departmentService.GetDepartmentSelectListAsync(departmentId);
            return View(vm);
        }
    }
}
