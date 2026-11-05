using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Filters;
using ERP_HRM_CMS.Helpers;
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
                .Include(m => m.SubMenus)
                .OrderBy(m => m.Position)
                .ThenBy(m => m.OrderIndex)
                .ToListAsync();

            ViewBag.IsSuperAdmin = LaSuperAdmin();
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

        /// <summary>
        /// Sửa tên Menu trực tiếp (Quick Rename) - Hỗ trợ cả AJAX và Form POST.
        /// Cho phép Admin Tổng và người có quyền menu.edit sửa tên ngay trên danh sách.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("menu.edit")]
        public async Task<IActionResult> DoiTen(int id, [FromForm] string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                if (LaYeuCauAjax()) return Json(new { success = false, message = "Tên menu không được để trống!" });
                TempData["ErrorMessage"] = "Tên menu không được để trống!";
                return RedirectToAction(nameof(Index));
            }

            var menu = await _context.Menus.FindAsync(id);
            if (menu == null)
            {
                if (LaYeuCauAjax()) return Json(new { success = false, message = "Không tìm thấy menu!" });
                return NotFound();
            }

            string tenCu = menu.Title;
            menu.Title = title.Trim();
            menu.UpdatedAt = VietnamTime.Now;
            menu.UpdatedBy = User.Identity?.Name ?? "Admin";

            await _context.SaveChangesAsync();

            if (LaYeuCauAjax())
            {
                return Json(new { success = true, message = $"Đã đổi tên menu từ '{tenCu}' sang '{menu.Title}' thành công!", title = menu.Title });
            }

            TempData["SuccessMessage"] = $"Đã đổi tên menu thành '{menu.Title}' thành công!";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Xóa Menu - Admin Tổng được phép xóa bất kỳ menu nào, kể cả menu cha có menu con (xóa đệ quy).
        /// Hỗ trợ cả AJAX JSON và Form POST.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("menu.delete")]
        public async Task<IActionResult> Delete(int id, bool force = false)
        {
            var menu = await _context.Menus
                .Include(m => m.SubMenus)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (menu == null)
            {
                if (LaYeuCauAjax()) return Json(new { success = false, message = "Không tìm thấy mục menu cần xóa!" });
                return NotFound();
            }

            bool isSuper = LaSuperAdmin();

            if (menu.SubMenus.Any())
            {
                if (isSuper || force)
                {
                    // Admin Tổng: Xóa đệ quy toàn bộ menu con
                    await XoaMenuDeQuyAsync(menu.Id);
                }
                else
                {
                    string msg = "Không thể xóa menu cha đang chứa các mục menu con! Vui lòng xóa menu con trước hoặc đăng nhập bằng Admin Tổng.";
                    if (LaYeuCauAjax()) return Json(new { success = false, message = msg });
                    TempData["ErrorMessage"] = msg;
                    return RedirectToAction(nameof(Index));
                }
            }

            string tenMenu = menu.Title;
            _context.Menus.Remove(menu);
            await _context.SaveChangesAsync();

            if (LaYeuCauAjax())
            {
                return Json(new { success = true, message = $"Đã xóa mục Menu '{tenMenu}' thành công!" });
            }

            TempData["SuccessMessage"] = $"Đã xóa mục Menu '{tenMenu}' thành công!";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>Xóa đệ quy toàn bộ menu con</summary>
        private async Task XoaMenuDeQuyAsync(int parentId)
        {
            var children = await _context.Menus
                .Include(m => m.SubMenus)
                .Where(m => m.ParentId == parentId)
                .ToListAsync();

            foreach (var child in children)
            {
                if (child.SubMenus.Any())
                {
                    await XoaMenuDeQuyAsync(child.Id);
                }
                _context.Menus.Remove(child);
            }
        }

        /// <summary>Kiểm tra quyền Admin Tổng / SuperAdmin</summary>
        private bool LaSuperAdmin()
        {
            return User.IsInRole("SUPERADMIN") ||
                   User.IsInRole("SuperAdmin") ||
                   string.Equals(User.FindFirst("IsSuperAdmin")?.Value, "True", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(User.Identity?.Name, "admin", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(User.Identity?.Name, "quantritong", StringComparison.OrdinalIgnoreCase);
        }

        private bool LaYeuCauAjax()
        {
            var req = Request;
            if (req == null) return false;
            return string.Equals(req.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase) ||
                   req.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase);
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
