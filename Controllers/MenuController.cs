using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Filters;
using ERP_HRM_CMS.Models.Entities;

namespace ERP_HRM_CMS.Controllers
{
    [Authorize]
    public class MenuController : Controller
    {
        private readonly AppDbContext _context;

        public MenuController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [KiemTraQuyen("menu.view")]
        public async Task<IActionResult> Index()
        {
            var menus = await _context.Menus
                .Include(m => m.Parent)
                .OrderBy(m => m.Position)
                .ThenBy(m => m.OrderIndex)
                .ToListAsync();

            return View(menus);
        }

        [HttpGet]
        [KiemTraQuyen("menu.create")]
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View(new Menu { Position = "MainNav", OrderIndex = 1, IsActive = true, Target = "_self" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("menu.create")]
        public async Task<IActionResult> Create(Menu menu)
        {
            if (ModelState.IsValid)
            {
                // Tự động sinh URL nếu link theo Content
                if (menu.LinkType == "Content" && menu.ReferenceId.HasValue)
                {
                    var content = await _context.Contents.FindAsync(menu.ReferenceId.Value);
                    if (content != null)
                    {
                        menu.CustomUrl = $"/bai-viet/{content.Slug}";
                    }
                }

                _context.Menus.Add(menu);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm mới Menu thành công!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns(menu.ParentId, menu.ReferenceId);
            return View(menu);
        }

        [HttpGet]
        [KiemTraQuyen("menu.edit")]
        public async Task<IActionResult> Edit(int id)
        {
            var menu = await _context.Menus.FindAsync(id);
            if (menu == null) return NotFound();

            await PopulateDropdowns(menu.ParentId, menu.ReferenceId);
            return View(menu);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("menu.edit")]
        public async Task<IActionResult> Edit(int id, Menu menu)
        {
            if (id != menu.Id) return NotFound();

            if (ModelState.IsValid)
            {
                if (menu.LinkType == "Content" && menu.ReferenceId.HasValue)
                {
                    var content = await _context.Contents.FindAsync(menu.ReferenceId.Value);
                    if (content != null)
                    {
                        menu.CustomUrl = $"/bai-viet/{content.Slug}";
                    }
                }

                _context.Update(menu);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật Menu thành công!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns(menu.ParentId, menu.ReferenceId);
            return View(menu);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("menu.delete")]
        public async Task<IActionResult> Delete(int id)
        {
            var menu = await _context.Menus.Include(m => m.SubMenus).FirstOrDefaultAsync(m => m.Id == id);
            if (menu == null) return NotFound();

            if (menu.SubMenus.Any())
            {
                TempData["ErrorMessage"] = "Không thể xóa menu cha đang chứa các mục menu con!";
                return RedirectToAction(nameof(Index));
            }

            _context.Menus.Remove(menu);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã xóa mục Menu thành công!";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdowns(int? selectedParentId = null, int? selectedRefId = null)
        {
            var parents = await _context.Menus
                .Where(m => m.ParentId == null)
                .OrderBy(m => m.Title)
                .ToListAsync();
            ViewBag.Parents = new SelectList(parents, "Id", "Title", selectedParentId);

            var contents = await _context.Contents
                .Where(c => c.Status == "Published")
                .OrderBy(c => c.Title)
                .ToListAsync();
            ViewBag.Contents = new SelectList(contents, "Id", "Title", selectedRefId);
        }
    }
}