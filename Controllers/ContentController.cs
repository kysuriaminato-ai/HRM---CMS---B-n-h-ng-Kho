using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Filters;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.ViewModels;

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
            return View(new ContentFormViewModel { Status = "Published" });
        }

        // Chỉ nhận các trường trong ContentFormViewModel; AuthorId lấy từ claim đăng nhập
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("content.create")]
        public async Task<IActionResult> Create(ContentFormViewModel model)
        {
            if (ModelState.IsValid)
            {
                var now = VietnamTime.Now;
                var content = new Content
                {
                    AuthorId = CurrentUserId(),
                    CreatedAt = now,
                    UpdatedAt = now
                };
                model.GhiVaoEntity(content);

                _context.Contents.Add(content);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xuất bản bài viết thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        [HttpGet]
        [KiemTraQuyen("content.edit")]
        public async Task<IActionResult> Edit(int id)
        {
            var content = await _context.Contents.FindAsync(id);
            if (content == null) return NotFound();
            return View(ContentFormViewModel.TuEntity(content));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("content.edit")]
        public async Task<IActionResult> Edit(int id, ContentFormViewModel model)
        {
            if (id != model.Id) return NotFound();

            // Tải entity từ database; không ghi đè Id, AuthorId, CreatedAt
            var content = await _context.Contents.FindAsync(id);
            if (content == null) return NotFound();

            if (ModelState.IsValid)
            {
                model.GhiVaoEntity(content);
                content.UpdatedAt = VietnamTime.Now;

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
            return View(model);
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

        private int? CurrentUserId()
        {
            return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : (int?)null;
        }
    }
}
