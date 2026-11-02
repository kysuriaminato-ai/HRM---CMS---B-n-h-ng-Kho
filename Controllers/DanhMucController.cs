using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Filters;
using ERP_HRM_CMS.Models.Entities;

namespace ERP_HRM_CMS.Controllers
{
    [Authorize]
    public class DanhMucController : Controller
    {
        private readonly AppDbContext _context;

        public DanhMucController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [KiemTraQuyen("product.view")]
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .Include(c => c.Parent)
                .Include(c => c.Products)
                .OrderBy(c => c.OrderIndex)
                .ToListAsync();

            ViewBag.Parents = await _context.Categories.Where(c => c.ParentId == null).ToListAsync();
            return View(categories);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("product.manage")]
        public async Task<IActionResult> Create(Category cat)
        {
            if (string.IsNullOrWhiteSpace(cat.Slug))
            {
                cat.Slug = cat.Code.ToLower().Replace(" ", "-");
            }

            if (await _context.Categories.AnyAsync(c => c.Code == cat.Code))
            {
                TempData["ErrorMessage"] = "Mã danh mục đã tồn tại!";
                return RedirectToAction(nameof(Index));
            }

            cat.CreatedAt = DateTime.Now;
            _context.Categories.Add(cat);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Thêm danh mục '{cat.Name}' thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("product.manage")]
        public async Task<IActionResult> Delete(int id)
        {
            var cat = await _context.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);
            if (cat == null) return NotFound();

            if (cat.Products.Any())
            {
                TempData["ErrorMessage"] = "Không thể xóa danh mục đang chứa sản phẩm liên kết!";
                return RedirectToAction(nameof(Index));
            }

            _context.Categories.Remove(cat);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã xóa danh mục thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}