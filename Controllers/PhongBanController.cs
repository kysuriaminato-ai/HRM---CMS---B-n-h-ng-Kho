using ERP_HRM_CMS.Filters;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Services.HRM;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP_HRM_CMS.Controllers
{
    [Authorize]
    public class PhongBanController : Controller
    {
        private readonly IDepartmentService _departmentService;

        public PhongBanController(IDepartmentService departmentService)
        {
            _departmentService = departmentService;
        }

        // ═══════════════════════════════════════════════════════════
        // DANH SÁCH PHÒNG BAN (cây đệ quy & sơ đồ tổ chức tương tác)
        // ═══════════════════════════════════════════════════════════
        [HttpGet]
        [KiemTraQuyen("hrm.employee.view")]
        public async Task<IActionResult> Index()
        {
            ViewBag.Branches = await _departmentService.GetBranchesAsync();
            ViewBag.Positions = await _departmentService.GetPositionsAsync();

            // Lấy danh sách phẳng cho dropdown parent
            ViewBag.FlatDepartments = await _departmentService.GetFlatListAsync();

            // Lấy cây phòng ban
            var tree = await _departmentService.GetTreeAsync();
            return View(tree);
        }

        // ═══════════════════════════════════════════════════════════
        // CHI TIẾT PHÒNG BAN (DRAWER SLIDEOUT API)
        // ═══════════════════════════════════════════════════════════
        [HttpGet]
        [KiemTraQuyen("hrm.employee.view")]
        public async Task<IActionResult> GetDrawerDetail(int id)
        {
            var detail = await _departmentService.GetDrawerDetailAsync(id);
            if (detail == null) return NotFound(new { success = false, message = "Không tìm thấy phòng ban." });
            return Json(new { success = true, data = detail });
        }

        // ═══════════════════════════════════════════════════════════
        // TẠO PHÒNG BAN
        // ═══════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> TaoPhongBan(Department dept)
        {
            if (ModelState.IsValid)
            {
                var (success, error) = await _departmentService.CreateAsync(dept);
                if (success)
                    TempData["SuccessMessage"] = $"Thêm phòng ban '{dept.Name}' thành công!";
                else
                    TempData["ErrorMessage"] = error;
            }
            return RedirectToAction(nameof(Index));
        }

        // ═══════════════════════════════════════════════════════════
        // SỬA PHÒNG BAN
        // ═══════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> SuaPhongBan(int id, Department model)
        {
            var (success, error) = await _departmentService.UpdateAsync(id, model);
            if (success)
                TempData["SuccessMessage"] = $"Cập nhật phòng ban '{model.Name}' thành công!";
            else
                TempData["ErrorMessage"] = error;

            return RedirectToAction(nameof(Index));
        }

        // ═══════════════════════════════════════════════════════════
        // BẬT / TẮT HOẠT ĐỘNG
        // ═══════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var (success, error) = await _departmentService.ToggleActiveAsync(id);
            if (success)
                TempData["SuccessMessage"] = "Đã cập nhật trạng thái phòng ban!";
            else
                TempData["ErrorMessage"] = error;

            return RedirectToAction(nameof(Index));
        }

        // ═══════════════════════════════════════════════════════════
        // DI CHUYỂN PHÒNG BAN (đổi parent)
        // ═══════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> Move(int id, int newParentId, int newBranchId)
        {
            var (success, error) = await _departmentService.MoveAsync(id, newParentId, newBranchId);
            if (success)
                TempData["SuccessMessage"] = "Đã di chuyển phòng ban thành công!";
            else
                TempData["ErrorMessage"] = error;

            return RedirectToAction(nameof(Index));
        }

        // ═══════════════════════════════════════════════════════════
        // TẠO CHỨC VỤ
        // ═══════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> TaoChucVu(Position pos)
        {
            if (ModelState.IsValid)
            {
                var (success, error) = await _departmentService.CreatePositionAsync(pos);
                if (success)
                    TempData["SuccessMessage"] = $"Thêm chức vụ '{pos.Name}' thành công!";
                else
                    TempData["ErrorMessage"] = error;
            }
            return RedirectToAction(nameof(Index));
        }

        // ═══════════════════════════════════════════════════════════
        // XÓA PHÒNG BAN
        // ═══════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> XoaPhongBan(int id)
        {
            var (success, error) = await _departmentService.DeleteAsync(id);
            if (success)
                TempData["SuccessMessage"] = "Đã xóa phòng ban thành công!";
            else
                TempData["ErrorMessage"] = error;

            return RedirectToAction(nameof(Index));
        }
    }
}
