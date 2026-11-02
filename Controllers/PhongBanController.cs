using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Filters;
using ERP_HRM_CMS.Models.Entities;

namespace ERP_HRM_CMS.Controllers
{
    [Authorize]
    public class PhongBanController : Controller
    {
        private readonly AppDbContext _context;

        public PhongBanController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [KiemTraQuyen("hrm.employee.view")]
        public async Task<IActionResult> Index()
        {
            ViewBag.Branches = await _context.Branches.Include(b => b.Company).ToListAsync();
            ViewBag.Positions = await _context.Positions.OrderBy(p => p.Name).ToListAsync();

            var departments = await _context.Departments
                .Include(d => d.Branch)
                .Include(d => d.Employees)
                .OrderBy(d => d.BranchId)
                .ThenBy(d => d.Code)
                .ToListAsync();

            return View(departments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> TaoPhongBan(Department dept)
        {
            if (ModelState.IsValid)
            {
                dept.CreatedAt = DateTime.Now;
                _context.Departments.Add(dept);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Thêm phòng ban '{dept.Name}' thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> TaoChucVu(Position pos)
        {
            if (ModelState.IsValid)
            {
                pos.CreatedAt = DateTime.Now;
                _context.Positions.Add(pos);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Thêm chức vụ '{pos.Name}' thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

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
