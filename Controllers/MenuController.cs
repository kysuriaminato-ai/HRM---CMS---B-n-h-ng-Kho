using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Filters;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.ViewModels;

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
            return View(new MenuFormViewModel { Position = "MainNav", OrderIndex = 1, IsActive = true, Target = "_self" });
        }

        // Chỉ nhận các trường trong MenuFormViewModel; Id và trường hệ thống không đến từ client
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("menu.create")]
        public async Task<IActionResult> Create(MenuFormViewModel model)
        {
            if (ModelState.IsValid)
            {
                var menu = new Menu();
                model.GhiVaoEntity(menu);
                await GanUrlTheoBaiVietAsync(menu);

                _context.Menus.Add(menu);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm mới Menu thành công!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns(model.ParentId, model.ReferenceId);
            return View(model);
        }

        [HttpGet]
        [KiemTraQuyen("menu.edit")]
        public async Task<IActionResult> Edit(int id)
        {
            var menu = await _context.Menus.FindAsync(id);
            if (menu == null) return NotFound();

            await PopulateDropdowns(menu.ParentId, menu.ReferenceId);
            // Dùng chung form với Create; form biết đang sửa qua Model.Id
            return View("Create", MenuFormViewModel.TuEntity(menu));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("menu.edit")]
        public async Task<IActionResult> Edit(int id, MenuFormViewModel model)
        {
            if (id != model.Id) return NotFound();

            // Tải entity từ database rồi mới ghi các trường được phép (không ghi đè CreatedAt...)
            var menu = await _context.Menus.FindAsync(id);
            if (menu == null) return NotFound();

            if (ModelState.IsValid)
            {
                model.GhiVaoEntity(menu);
                await GanUrlTheoBaiVietAsync(menu);

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật Menu thành công!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns(model.ParentId, model.ReferenceId);
            return View("Create", model);
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

        /// <summary>Liên kết theo bài viết: URL được sinh từ slug, không nhận từ người dùng.</summary>
        private async Task GanUrlTheoBaiVietAsync(Menu menu)
        {
            if (menu.LinkType == "Content" && menu.ReferenceId.HasValue)
            {
                var content = await _context.Contents.FindAsync(menu.ReferenceId.Value);
                if (content != null)
                {
                    menu.CustomUrl = $"/bai-viet/{content.Slug}";
                }
            }
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
