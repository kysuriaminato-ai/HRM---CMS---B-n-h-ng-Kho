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
    public class TaiSanNhanSuController : Controller
    {
        private readonly IAssetManagementService _assetService;
        private readonly ICurrentUserService _currentUser;

        public TaiSanNhanSuController(
            IAssetManagementService assetService,
            ICurrentUserService currentUser)
        {
            _assetService = assetService;
            _currentUser = currentUser;
        }

        private int? CurrentUserId => _currentUser.UserId;

        // ═══════════════════════════════════════════════════════════
        // DANH MỤC TÀI SẢN DOANH NGHIỆP
        // ═══════════════════════════════════════════════════════════
        [HttpGet]
        [KiemTraQuyen("hrm.employee.view")]
        public async Task<IActionResult> Index(
            string? keyword, string? assetType, string? status, int? branchId, int page = 1, int pageSize = 20)
        {
            var filter = new AssetFilterDto
            {
                Keyword = keyword,
                AssetType = assetType,
                Status = status,
                BranchId = branchId,
                Page = page,
                PageSize = pageSize
            };

            var paged = await _assetService.GetAssetsPagedAsync(filter);
            ViewBag.Filter = filter;
            var dropdowns = await _assetService.GetDropdownsAsync();
            ViewBag.Branches = dropdowns.Branches;

            return View(paged);
        }

        // ═══════════════════════════════════════════════════════════
        // THÊM MỚI TÀI SẢN
        // ═══════════════════════════════════════════════════════════
        [HttpGet]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> CreateAsset()
        {
            var dropdowns = await _assetService.GetDropdownsAsync();
            ViewBag.Branches = dropdowns.Branches;

            return View(new CompanyAsset
            {
                AssetCode = MaChungTu.Sinh("TS"),
                AssetType = "Laptop",
                Status = "Available",
                PurchaseDate = VietnamTime.Today
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> CreateAsset(CompanyAsset asset)
        {
            if (ModelState.IsValid)
            {
                var (success, error, id) = await _assetService.CreateAssetAsync(asset, CurrentUserId);
                if (success)
                {
                    TempData["SuccessMessage"] = $"Thêm tài sản '{asset.AssetName}' thành công!";
                    return RedirectToAction(nameof(Details), new { id });
                }
                ModelState.AddModelError("", error ?? "Không thể thêm tài sản.");
            }

            var dropdowns = await _assetService.GetDropdownsAsync();
            ViewBag.Branches = dropdowns.Branches;
            return View(asset);
        }

        // ═══════════════════════════════════════════════════════════
        // CHI TIẾT TÀI SẢN & LỊCH SỬ CẤP PHÁT
        // ═══════════════════════════════════════════════════════════
        [HttpGet]
        [KiemTraQuyen("hrm.employee.view")]
        public async Task<IActionResult> Details(int id)
        {
            var asset = await _assetService.GetAssetByIdAsync(id);
            if (asset == null) return NotFound();

            ViewBag.Assignments = await _assetService.GetAssetAssignmentsAsync(id);
            return View(asset);
        }

        // ═══════════════════════════════════════════════════════════
        // CẤP PHÁT TÀI SẢN CHO NHÂN SỰ
        // ═══════════════════════════════════════════════════════════
        [HttpGet]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> CapPhat(int? assetId, int? employeeId)
        {
            var dropdowns = await _assetService.GetDropdownsAsync();
            ViewBag.AvailableAssets = dropdowns.AvailableAssets;
            ViewBag.Employees = dropdowns.Employees;
            ViewBag.SelectedAssetId = assetId;
            ViewBag.SelectedEmployeeId = employeeId;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> CapPhat(int assetId, int employeeId, string? conditionOnAssign, string? note)
        {
            var (success, error) = await _assetService.AssignAssetAsync(
                assetId, employeeId, conditionOnAssign, CurrentUserId, note);

            if (success)
            {
                TempData["SuccessMessage"] = "Bàn giao cấp phát tài sản cho nhân viên thành công!";
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = error;
            return RedirectToAction(nameof(CapPhat), new { assetId, employeeId });
        }

        // ═══════════════════════════════════════════════════════════
        // THU HỒI TÀI SẢN
        // ═══════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> ThuHoi(
            int assignmentId, string conditionOnReturn, string returnStatus, string? note, int? assetId = null)
        {
            var (success, error) = await _assetService.ReturnAssetAsync(
                assignmentId, conditionOnReturn, returnStatus, CurrentUserId, note);

            if (success)
            {
                TempData["SuccessMessage"] = "Thu hồi tài sản thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = error;
            }

            if (assetId.HasValue && assetId.Value > 0)
                return RedirectToAction(nameof(Details), new { id = assetId.Value });

            return RedirectToAction(nameof(Index));
        }

        // ═══════════════════════════════════════════════════════════
        // KIỂM TRA TÀI SẢN CHƯA BÀN GIAO (AJAX KIỂM SOÁT THÔI VIỆC)
        // ═══════════════════════════════════════════════════════════
        [HttpGet]
        [KiemTraQuyen("hrm.employee.view")]
        public async Task<IActionResult> KiemTraChuaTra(int employeeId)
        {
            bool hasUnreturned = await _assetService.HasUnreturnedAssetsAsync(employeeId);
            var unreturnedList = await _assetService.GetUnreturnedAssetsAsync(employeeId);

            return Json(new
            {
                hasUnreturned,
                count = unreturnedList.Count,
                items = unreturnedList
            });
        }
    }
}

