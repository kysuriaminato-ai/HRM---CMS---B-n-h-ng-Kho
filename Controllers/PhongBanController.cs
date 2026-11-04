using ERP_HRM_CMS.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Filters;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Services.HRM;

namespace ERP_HRM_CMS.Controllers
{
    [Authorize]
    public class PhongBanController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IDepartmentService _departmentService;

        public PhongBanController(AppDbContext context, IDepartmentService departmentService)
        {
            _context = context;
            _departmentService = departmentService;
        }

        // ═══════════════════════════════════════════════════════════
        // DANH SÁCH PHÒNG BAN (cây đệ quy)
        // ═══════════════════════════════════════════════════════════
        [HttpGet]
        [KiemTraQuyen("hrm.employee.view")]
        public async Task<IActionResult> Index()
        {
            ViewBag.Branches = await _context.Branches.Include(b => b.Company).ToListAsync();
            ViewBag.Positions = await _context.Positions.OrderBy(p => p.Name).ToListAsync();

            // Lấy danh sách phẳng cho dropdown parent
            ViewBag.FlatDepartments = await _departmentService.GetFlatListAsync();

            // Lấy cây phòng ban
            var tree = await _departmentService.GetTreeAsync();
            return View(tree);
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
                pos.CreatedAt = VietnamTime.Now;
                _context.Positions.Add(pos);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Thêm chức vụ '{pos.Name}' thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        // ═══════════════════════════════════════════════════════════
        // XÓA PHÒNG BAN (soft delete / hard delete nếu không có NV)
        // ═══════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> XoaPhongBan(int id)
        {
            var dept = await _context.Departments.Include(d => d.Employees).FirstOrDefaultAsync(d => d.Id == id);
            if (dept == null) return NotFound();

            if (dept.Employees.Any())
            {
                TempData["ErrorMessage"] = "Không thể xóa phòng ban đang có nhân viên trực thuộc!";
                return RedirectToAction(nameof(Index));
            }

            _context.Departments.Remove(dept);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã xóa phòng ban thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}
