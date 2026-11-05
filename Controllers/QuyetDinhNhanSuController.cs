using ERP.Application.Common.Interfaces;
using ERP_HRM_CMS.Filters;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Services.HRM;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP_HRM_CMS.Controllers
{
    [Authorize]
    public class QuyetDinhNhanSuController : Controller
    {
        private readonly IHrDecisionService _decisionService;
        private readonly ICurrentUserService _currentUser;

        public QuyetDinhNhanSuController(
            IHrDecisionService decisionService,
            ICurrentUserService currentUser)
        {
            _decisionService = decisionService;
            _currentUser = currentUser;
        }

        private int? CurrentUserId => _currentUser.UserId;

        // ═══════════════════════════════════════════════════════════
        // DANH SÁCH QUYẾT ĐỊNH NHÂN SỰ
        // ═══════════════════════════════════════════════════════════
        [HttpGet]
        [KiemTraQuyen("hrm.employee.view")]
        public async Task<IActionResult> Index(
            string? keyword, string? decisionType, string? status, int? departmentId,
            DateTime? fromDate, DateTime? toDate, int page = 1, int pageSize = 20)
        {
            var filter = new HrDecisionFilterDto
            {
                Keyword = keyword,
                DecisionType = decisionType,
                Status = status,
                DepartmentId = departmentId,
                FromDate = fromDate,
                ToDate = toDate,
                Page = page,
                PageSize = pageSize
            };

            var paged = await _decisionService.GetPagedAsync(filter);
            ViewBag.Filter = filter;
            var dropdowns = await _decisionService.GetDropdownsAsync();
            ViewBag.Departments = dropdowns.Departments;

            return View(paged);
        }

        // ═══════════════════════════════════════════════════════════
        // CHI TIẾT QUYẾT ĐỊNH
        // ═══════════════════════════════════════════════════════════
        [HttpGet]
        [KiemTraQuyen("hrm.employee.view")]
        public async Task<IActionResult> Details(int id)
        {
            var decision = await _decisionService.GetByIdAsync(id);
            if (decision == null) return NotFound();

            return View(decision);
        }

        // ═══════════════════════════════════════════════════════════
        // TẠO MỚI QUYẾT ĐỊNH
        // ═══════════════════════════════════════════════════════════
        [HttpGet]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> Create(int? employeeId, string? type)
        {
            var dropdowns = await _decisionService.GetDropdownsAsync();
            ViewBag.Employees = dropdowns.Employees;
            ViewBag.Departments = dropdowns.Departments;
            ViewBag.Positions = dropdowns.Positions;

            return View(new HrDecision
            {
                EmployeeId = employeeId ?? 0,
                DecisionNumber = MaChungTu.Sinh("QD"),
                DecisionType = type ?? "Appointment",
                Title = "Quyết định về việc ...",
                EffectiveDate = VietnamTime.Today,
                SignDate = VietnamTime.Today,
                Status = "Issued",
                SignerName = "Nguyễn Văn Giám Đốc",
                SignerTitle = "Tổng Giám đốc"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> Create(HrDecision decision)
        {
            if (ModelState.IsValid)
            {
                var (success, error, id) = await _decisionService.CreateAsync(decision, CurrentUserId);
                if (success)
                {
                    TempData["SuccessMessage"] = $"Tạo quyết định số {decision.DecisionNumber} thành công!";
                    return RedirectToAction(nameof(Details), new { id });
                }
                ModelState.AddModelError("", error ?? "Không thể tạo quyết định.");
            }

            var dropdowns = await _decisionService.GetDropdownsAsync();
            ViewBag.Employees = dropdowns.Employees;
            ViewBag.Departments = dropdowns.Departments;
            ViewBag.Positions = dropdowns.Positions;
            return View(decision);
        }

        // ═══════════════════════════════════════════════════════════
        // PHÊ DUYỆT QUYẾT ĐỊNH (Tự động cập nhật nhân sự + Transaction + SignalR)
        // ═══════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> Approve(int id)
        {
            int approvedBy = CurrentUserId ?? 1;
            var (success, error) = await _decisionService.ApproveDecisionAsync(id, approvedBy);

            if (success)
            {
                TempData["SuccessMessage"] = "Phê duyệt quyết định nhân sự và áp dụng hiệu lực thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = error;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // ═══════════════════════════════════════════════════════════
        // HỦY QUYẾT ĐỊNH
        // ═══════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> Cancel(int id, string? reason)
        {
            int cancelledBy = CurrentUserId ?? 1;
            var (success, error) = await _decisionService.CancelDecisionAsync(id, cancelledBy, reason);

            if (success)
            {
                TempData["SuccessMessage"] = "Đã hủy quyết định nhân sự.";
            }
            else
            {
                TempData["ErrorMessage"] = error;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // ═══════════════════════════════════════════════════════════
        // IN QUYẾT ĐỊNH CHUẨN THỂ THỨC VĂN BẢN HÀNH CHÍNH VIỆT NAM
        // ═══════════════════════════════════════════════════════════
        [HttpGet]
        [KiemTraQuyen("hrm.employee.view")]
        public async Task<IActionResult> InQuyetDinh(int id)
        {
            var printModel = await _decisionService.GetPrintModelAsync(id);
            if (printModel == null) return NotFound();
            return View(printModel);
        }
    }
}

