using ERP.Application.Common.Interfaces;
using ERP.Domain.Enums;
using ERP_HRM_CMS.Filters;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.ViewModels;
using ERP_HRM_CMS.Services.HRM;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP_HRM_CMS.Controllers
{
    [Authorize]
    public class DonTuController : Controller
    {
        private readonly ILeaveManagementService _leaveService;
        private readonly IDepartmentService _departmentService;
        private readonly IEmployeeService _employeeService;
        private readonly ICurrentUserService _currentUser;
        private readonly IPermissionService _permissionService;

        public DonTuController(
            ILeaveManagementService leaveService,
            IDepartmentService departmentService,
            IEmployeeService employeeService,
            ICurrentUserService currentUser,
            IPermissionService permissionService)
        {
            _leaveService = leaveService;
            _departmentService = departmentService;
            _employeeService = employeeService;
            _currentUser = currentUser;
            _permissionService = permissionService;
        }

        private int CurrentUserId => _currentUser.GetRequiredUserId();

        // ═══════════════════════════════════════════════════════════
        // 1. DANH SÁCH ĐƠN TỪ & TRUNG TÂM PHÊ DUYỆT
        // ═══════════════════════════════════════════════════════════
        [HttpGet]
        [KiemTraQuyen("hrm.leave.view,hrm.self.service")]
        public async Task<IActionResult> Index(
            string? status, string? type, int? departmentId, DateTime? fromDate, DateTime? toDate, string? keyword, int page = 1)
        {
            var filter = new LeaveRequestFilterDto
            {
                Status = status,
                Type = type,
                DepartmentId = departmentId,
                FromDate = fromDate,
                ToDate = toDate,
                Keyword = keyword,
                Page = page,
                PageSize = 20
            };

            var paged = await _leaveService.GetLeaveRequestsPagedAsync(filter);
            var kpis = await _leaveService.GetSummaryKpisAsync();

            ViewBag.Filter = filter;
            ViewBag.TotalCount = kpis.TotalCount;
            ViewBag.PendingCount = kpis.PendingCount;
            ViewBag.DeptApprovedCount = kpis.DeptApprovedCount;
            ViewBag.ApprovedCount = kpis.ApprovedCount;
            ViewBag.RejectedCount = kpis.RejectedCount;

            ViewBag.CanApproveLeave = _currentUser.IsSuperAdmin ||
                                     await _permissionService.HasPermissionAsync(CurrentUserId, "hrm.leave.approve");
            ViewBag.CurrentEmployeeId = _currentUser.EmployeeId;
            ViewBag.CurrentUserId = CurrentUserId;
            ViewBag.IsSuperAdmin = _currentUser.IsSuperAdmin;
            ViewBag.Departments = await _departmentService.GetDepartmentSelectListAsync(departmentId);
            ViewBag.LeaveTypes = await _leaveService.GetLeaveTypesAsync();

            return View(paged);
        }

        // ═══════════════════════════════════════════════════════════
        // 2. TẠO ĐƠN TỪ MỚI (ESS & HR)
        // ═══════════════════════════════════════════════════════════
        [HttpGet]
        [KiemTraQuyen("hrm.leave.manage,hrm.self.service")]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns(_currentUser.EmployeeId);
            var model = new LeaveRequest
            {
                EmployeeId = _currentUser.EmployeeId ?? 0,
                FromDate = VietnamTime.Today,
                ToDate = VietnamTime.Today,
                TotalDays = 1.0m,
                RequestType = "ANNUAL_LEAVE"
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.leave.manage,hrm.self.service")]
        public async Task<IActionResult> Create(LeaveRequest model)
        {
            ModelState.Remove(nameof(LeaveRequest.Employee));

            // Nếu phạm vi là Self, ép EmployeeId vào chính mình
            if (_currentUser.EmployeeId.HasValue)
            {
                model.EmployeeId = _currentUser.EmployeeId.Value;
            }

            if (ModelState.IsValid)
            {
                var (success, error, _) = await _leaveService.CreateLeaveRequestAsync(model, CurrentUserId);
                if (success)
                {
                    TempData["SuccessMessage"] = $"Đã nộp đơn {model.RequestCode} thành công và gửi thông báo đến người phê duyệt!";
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError("", error ?? "Không thể gửi đơn từ.");
            }

            await LoadDropdowns(model.EmployeeId);
            return View(model);
        }

        // ═══════════════════════════════════════════════════════════
        // 3. DUYỆT ĐƠN (CẤP 1: TP / CẤP 2: GD HOẶC HR)
        // ═══════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.leave.approve")]
        public async Task<IActionResult> Duyet(int id, string level, string? note)
        {
            bool isAjax = IsAjaxRequest();
            var (success, error) = await _leaveService.ApproveLeaveRequestAsync(id, level, note, CurrentUserId);

            if (isAjax)
            {
                if (success) return Json(new { success = true, message = "Phê duyệt đơn từ thành công!" });
                if (error != null && (error.Contains("kiểm soát nội bộ") || error.Contains("tự phê duyệt")))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = error });
                }
                return BadRequest(new { success = false, message = error });
            }

            if (success)
                TempData["SuccessMessage"] = "Phê duyệt đơn từ thành công!";
            else
                TempData["ErrorMessage"] = error;

            return RedirectToAction(nameof(Index));
        }

        // ═══════════════════════════════════════════════════════════
        // 4. DUYỆT HÀNG LOẠT (BATCH APPROVAL)
        // ═══════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.leave.approve")]
        public async Task<IActionResult> DuyetHangLoat(List<int> ids, string level, string? note)
        {
            if (ids == null || !ids.Any())
            {
                return Json(new { success = false, message = "Vui lòng chọn ít nhất 1 đơn từ để duyệt." });
            }

            var (successCount, failCount, errors) = await _leaveService.BatchApproveAsync(
                ids, level, note, CurrentUserId);

            return Json(new
            {
                success = true,
                successCount,
                failCount,
                message = $"Đã phê duyệt {successCount}/{ids.Count} đơn từ được chọn!"
            });
        }

        // ═══════════════════════════════════════════════════════════
        // 5. TỪ CHỐI ĐƠN TỪ
        // ═══════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.leave.approve")]
        public async Task<IActionResult> TuChoi(int id, string reason)
        {
            bool isAjax = IsAjaxRequest();
            var (success, error) = await _leaveService.RejectLeaveRequestAsync(id, reason, CurrentUserId);

            if (isAjax)
            {
                if (success) return Json(new { success = true, message = "Đã từ chối đơn từ." });
                if (error != null && (error.Contains("kiểm soát nội bộ") || error.Contains("tự phê duyệt") || error.Contains("tự từ chối")))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = error });
                }
                return BadRequest(new { success = false, message = error });
            }

            if (success)
                TempData["SuccessMessage"] = "Đã từ chối đơn từ!";
            else
                TempData["ErrorMessage"] = error;

            return RedirectToAction(nameof(Index));
        }

        // ═══════════════════════════════════════════════════════════
        // 6. QUẢN LÝ QUỸ PHÉP NĂM (LEAVE BALANCES)
        // ═══════════════════════════════════════════════════════════
        [HttpGet]
        [KiemTraQuyen("hrm.leave.view")]
        public async Task<IActionResult> QuyPhepNam(int? year, int? departmentId, string? keyword, int page = 1)
        {
            int targetYear = year ?? VietnamTime.Today.Year;
            var filter = new LeaveBalanceFilterDto
            {
                Year = targetYear,
                DepartmentId = departmentId,
                Keyword = keyword,
                Page = page,
                PageSize = 25
            };

            var paged = await _leaveService.GetLeaveBalancesPagedAsync(filter);
            ViewBag.Filter = filter;
            ViewBag.Departments = await _departmentService.GetDepartmentSelectListAsync(departmentId);
            return View(paged);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.leave.manage")]
        public async Task<IActionResult> KhoiTaoQuyPhep(int year)
        {
            var (success, error, count) = await _leaveService.RecalculateAllBalancesAsync(year, CurrentUserId);
            if (success)
            {
                TempData["SuccessMessage"] = $"Đã tính toán & khởi tạo thành công Quỹ phép năm {year} (kèm phép thâm niên BLLĐ) cho {count} nhân sự!";
            }
            else
            {
                TempData["ErrorMessage"] = error;
            }

            return RedirectToAction(nameof(QuyPhepNam), new { year });
        }

        // ═══════════════════════════════════════════════════════════
        // 7. ĐƠN GIẢI TRÌNH CHẤM CÔNG (ATTENDANCE ADJUSTMENT)
        // ═══════════════════════════════════════════════════════════
        [HttpGet]
        [KiemTraQuyen("hrm.leave.view,hrm.self.service")]
        public async Task<IActionResult> GiaiTrinh(string? status, string? type, int? departmentId, DateTime? workDate, int page = 1)
        {
            var filter = new AdjustmentFilterDto
            {
                Status = status,
                AdjustmentType = type,
                DepartmentId = departmentId,
                WorkDate = workDate,
                Page = page,
                PageSize = 20
            };

            var paged = await _leaveService.GetAdjustmentRequestsPagedAsync(filter);
            ViewBag.Filter = filter;
            ViewBag.Departments = await _departmentService.GetDepartmentSelectListAsync(departmentId);
            ViewBag.CanApprove = _currentUser.IsSuperAdmin ||
                                 await _permissionService.HasPermissionAsync(CurrentUserId, "hrm.attendance.manage");
            return View(paged);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.leave.manage,hrm.self.service")]
        public async Task<IActionResult> CreateGiaiTrinh(AttendanceAdjustmentRequest request)
        {
            if (_currentUser.EmployeeId.HasValue)
                request.EmployeeId = _currentUser.EmployeeId.Value;

            if (ModelState.IsValid)
            {
                var (success, error, _) = await _leaveService.CreateAdjustmentRequestAsync(request, CurrentUserId);
                if (success)
                {
                    TempData["SuccessMessage"] = $"Gửi đơn giải trình chấm công {request.RequestCode} thành công!";
                    return RedirectToAction(nameof(GiaiTrinh));
                }
                TempData["ErrorMessage"] = error;
            }

            return RedirectToAction(nameof(GiaiTrinh));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.attendance.manage")]
        public async Task<IActionResult> DuyetGiaiTrinh(int id, string? note)
        {
            var (success, error) = await _leaveService.ApproveAdjustmentRequestAsync(id, CurrentUserId, note);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success, message = success ? "Duyệt giải trình thành công!" : error });

            if (success)
                TempData["SuccessMessage"] = "Phê duyệt đơn giải trình và đồng bộ giờ công thành công!";
            else
                TempData["ErrorMessage"] = error;

            return RedirectToAction(nameof(GiaiTrinh));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.attendance.manage")]
        public async Task<IActionResult> TuChoiGiaiTrinh(int id, string reason)
        {
            var (success, error) = await _leaveService.RejectAdjustmentRequestAsync(id, reason, CurrentUserId);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success, message = success ? "Từ chối giải trình thành công!" : error });

            if (success)
                TempData["SuccessMessage"] = "Đã từ chối đơn giải trình chấm công.";
            else
                TempData["ErrorMessage"] = error;

            return RedirectToAction(nameof(GiaiTrinh));
        }

        // ═══════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════

        private async Task LoadDropdowns(int? selectedEmpId = null)
        {
            var dropdowns = await _employeeService.GetDropdownDataAsync();
            ViewBag.Employees = dropdowns.Employees;
            ViewBag.LeaveTypes = await _leaveService.GetLeaveTypesAsync();
        }

        private bool IsAjaxRequest()
        {
            return string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase)
                || Request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase);
        }
    }
}
