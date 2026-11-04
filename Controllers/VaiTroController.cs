using ERP_HRM_CMS.Helpers;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Filters;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.ViewModels;

namespace ERP_HRM_CMS.Controllers
{
    /// <summary>
    /// Quản lý nhóm quyền (vai trò) do SuperAdmin tạo.
    /// Sau khi tạo, gán quyền chi tiết tại PhanQuyen/MaTranPhanQuyen,
    /// rồi gán nhóm cho tài khoản tại QuanTriTaiKhoan.
    /// </summary>
    [Authorize]
    [KiemTraQuyen("auth.user.manage")]
    public class VaiTroController : Controller
    {
        private static readonly Regex MaNhomHopLe = new(@"^[A-Z][A-Z0-9_]{2,49}$", RegexOptions.Compiled);
        private readonly AppDbContext _context;

        public VaiTroController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var roles = await _context.Roles
                .Include(r => r.UserRoles)
                .Include(r => r.RolePermissions)
                .OrderBy(r => r.IsSystem ? 0 : 1)
                .ThenBy(r => r.Name)
                .ToListAsync();
            return View(roles);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new VaiTroFormModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VaiTroFormModel model)
        {
            model.Code = (model.Code ?? string.Empty).Trim().ToUpperInvariant();
            model.Name = (model.Name ?? string.Empty).Trim();
            model.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();

            await KiemTraHopLeAsync(model, dangSua: false);
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var role = new Role
            {
                Code = model.Code,
                Name = model.Name,
                Description = model.Description,
                IsSystem = false,
                IsActive = true,
                CreatedAt = VietnamTime.Now
            };
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            TempData["ThongBao"] = $"Đã tạo nhóm quyền \"{role.Name}\". Hãy gán quyền cho nhóm này ngay bên dưới.";
            return RedirectToAction("MaTranPhanQuyen", "PhanQuyen", new { roleId = role.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            return View(new VaiTroFormModel
            {
                Id = role.Id,
                Code = role.Code,
                Name = role.Name,
                Description = role.Description,
                IsSystem = role.IsSystem
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(VaiTroFormModel model)
        {
            var role = await _context.Roles.FindAsync(model.Id);
            if (role == null)
            {
                return NotFound();
            }

            // Mã nhóm không đổi sau khi tạo, vì mã được dùng trong code và phân quyền.
            model.Code = role.Code;
            model.IsSystem = role.IsSystem;
            model.Name = (model.Name ?? string.Empty).Trim();
            model.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
            ModelState.Remove(nameof(VaiTroFormModel.Code));

            if (string.IsNullOrEmpty(model.Name))
            {
                ModelState.AddModelError(nameof(VaiTroFormModel.Name), "Vui lòng nhập tên nhóm quyền.");
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            role.Name = model.Name;
            role.Description = model.Description;
            await _context.SaveChangesAsync();

            TempData["ThongBao"] = $"Đã cập nhật nhóm quyền \"{role.Name}\".";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var role = await _context.Roles
                .Include(r => r.UserRoles)
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (role == null)
            {
                return NotFound();
            }

            if (role.IsSystem)
            {
                TempData["LoiVaiTro"] = "Không thể xóa nhóm quyền mặc định của hệ thống.";
                return RedirectToAction(nameof(Index));
            }
            if (role.UserRoles.Count > 0)
            {
                TempData["LoiVaiTro"] = $"Nhóm \"{role.Name}\" đang được gán cho {role.UserRoles.Count} tài khoản. Hãy gỡ nhóm khỏi các tài khoản trước.";
                return RedirectToAction(nameof(Index));
            }

            _context.RolePermissions.RemoveRange(role.RolePermissions);
            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();

            TempData["ThongBao"] = $"Đã xóa nhóm quyền \"{role.Name}\".";
            return RedirectToAction(nameof(Index));
        }

        private async Task KiemTraHopLeAsync(VaiTroFormModel model, bool dangSua)
        {
            if (string.IsNullOrEmpty(model.Code))
            {
                ModelState.AddModelError(nameof(VaiTroFormModel.Code), "Vui lòng nhập mã nhóm quyền.");
            }
            else if (!MaNhomHopLe.IsMatch(model.Code))
            {
                ModelState.AddModelError(nameof(VaiTroFormModel.Code),
                    "Mã nhóm chỉ gồm chữ không dấu, số và dấu gạch dưới, bắt đầu bằng chữ, dài 3 đến 50 ký tự.");
            }
            else if (await _context.Roles.AnyAsync(r => r.Code == model.Code))
            {
                ModelState.AddModelError(nameof(VaiTroFormModel.Code), "Mã nhóm này đã tồn tại.");
            }

            if (string.IsNullOrEmpty(model.Name))
            {
                ModelState.AddModelError(nameof(VaiTroFormModel.Name), "Vui lòng nhập tên nhóm quyền.");
            }
        }
    }
}
