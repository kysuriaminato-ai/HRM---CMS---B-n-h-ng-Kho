using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Filters;
using ERP_HRM_CMS.Models.Entities;

namespace ERP_HRM_CMS.Controllers
{
    [Authorize]
    public class ContentController : Controller
    {
        private readonly AppDbContext _context;

        public ContentController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [KiemTraQuyen("content.view")]
        public async Task<IActionResult> Index()
        {
            var contents = await _context.Contents
                .Include(c => c.Author)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return View(contents);
        }

        [HttpGet]
        [KiemTraQuyen("content.create")]
        public IActionResult Create()
        {
            return View(new Content { Status = "Published" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("content.create")]
        public async Task<IActionResult> Create(Content content)
        {
            if (ModelState.IsValid)
            {
                content.AuthorId = HttpContext.Session.GetInt32("UserId");
                content.CreatedAt = DateTime.Now;
                content.UpdatedAt = DateTime.Now;

                _context.Contents.Add(content);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xuất bản bài viết thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(content);
        }

        [HttpGet]
        [KiemTraQuyen("content.edit")]
        public async Task<IActionResult> Edit(int id)
        {
            var content = await _context.Contents.FindAsync(id);
            if (content == null) return NotFound();
            return View(content);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("content.edit")]
        public async Task<IActionResult> Edit(int id, Content content)
        {
            if (id != content.Id) return NotFound();

            if (ModelState.IsValid)
            {
                content.UpdatedAt = DateTime.Now;
                _context.Update(content);

                // Anti-Broken Link: Nếu bài viết bị chuyển sang Draft/Archived, vô hiệu hóa Menu liên quan
                if (content.Status != "Published")
                {
                    var boundMenus = await _context.Menus
                        .Where(m => m.LinkType == "Content" && m.ReferenceId == content.Id)
                        .ToListAsync();

                    foreach (var m in boundMenus)
                    {
                        m.IsActive = false;
                    }
                    if (boundMenus.Any())
                    {
                        TempData["ErrorMessage"] = $"Lưu ý: Có {boundMenus.Count} mục Menu trỏ đến bài viết này đã được tự động ẩn để tránh liên kết gãy!";
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật bài viết thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(content);
        }

        // Xóa bài viết với cơ chế Anti-Broken Link
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("content.delete")]
        public async Task<IActionResult> Delete(int id)
        {
            var content = await _context.Contents.FindAsync(id);
            if (content == null) return NotFound();

            // Tìm kiếm các liên kết Menu phụ thuộc
            var boundMenus = await _context.Menus
                .Where(m => m.LinkType == "Content" && m.ReferenceId == id)
                .ToListAsync();

            if (boundMenus.Any())
            {
                foreach (var m in boundMenus)
                {
                    m.IsActive = false;
                    m.ReferenceId = null;
                    m.CustomUrl = "#lien-ket-da-xoa";
                }
            }

            _context.Contents.Remove(content);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = boundMenus.Any()
                ? $"Đã xóa bài viết và tự động gỡ liên kết an toàn cho {boundMenus.Count} mục Menu liên quan!"
                : "Đã xóa bài viết thành công!";

            return RedirectToAction(nameof(Index));
        }
    }
}
