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
    /// <summary>
    /// Quản lý tài khoản: duyệt đăng ký, tạo tài khoản, gán vai trò, khóa và đặt lại mật khẩu.
    /// Chỉ SuperAdmin có quyền "auth.user.manage" (xem DbInitializer).
    /// </summary>
    [Authorize]
    [KiemTraQuyen("auth.user.manage")]
    public class QuanTriTaiKhoanController : Controller
    {
        private const string MaSuperAdmin = "SUPERADMIN";
        private readonly AppDbContext _context;

        public QuanTriTaiKhoanController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? loc)
        {
            var query = _context.Users
                .Include(u => u.Employee)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .AsQueryable();

            if (loc == "khoa")
            {
                query = query.Where(u => !u.IsActive);
            }

            var users = await query.OrderBy(u => u.Username).ToListAsync();

            var vm = new TaiKhoanIndexViewModel
            {
                Items = users.Select(u => new TaiKhoanIndexItem
                {
                    Id = u.Id,
                    Username = u.Username,
                    FullName = u.Employee != null ? u.Employee.FullName : null,
                    IsActive = u.IsActive,
                    IsSuperAdmin = u.IsSuperAdmin,
                    LastLoginAt = u.LastLoginAt,
                    CreatedAt = u.CreatedAt,
                    RoleNames = u.UserRoles.Select(ur => ur.Role.Name).ToList()
                }).ToList(),
                SoChoDuyet = await _context.Users.CountAsync(u => !u.IsActive)
            };

            ViewBag.Loc = loc;
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var vm = new TaiKhoanFormViewModel
            {
                IsActive = true,
                Roles = await LoadRolesAsync(new List<int>())
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaiKhoanFormViewModel model, List<int>? roleIds)
        {
            string tenDangNhap = (model.Username ?? string.Empty).Trim();
            var chon = (roleIds ?? new List<int>()).Distinct().ToList();

            string? loi;
            if (tenDangNhap.Length < 3 || tenDangNhap.Length > 50)
            {
                loi = "Tên đăng nhập từ 3 đến 50 ký tự.";
            }
            else if (await _context.Users.AnyAsync(u => u.Username == tenDangNhap))
            {
                loi = "Tên đăng nhập đã tồn tại.";
            }
            else
            {
                loi = MatKhauHelper.KiemTraDoManh(model.Password);
            }

            loi ??= await KiemTraVaiTroAsync(chon);

            if (loi != null)
            {
                ModelState.AddModelError(string.Empty, loi);
                model.Roles = await LoadRolesAsync(chon);
                return View(model);
            }

            string salt = MatKhauHelper.GenerateSalt();
            var user = new User
            {
                Username = tenDangNhap,
                PasswordSalt = salt,
                PasswordHash = MatKhauHelper.HashPassword(model.Password!, salt),
                IsActive = model.IsActive,
                IsSuperAdmin = false,
                CreatedAt = DateTime.Now
            };

            foreach (var roleId in chon)
            {
                user.UserRoles.Add(new UserRole { RoleId = roleId });
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["ThongBao"] = $"Đã tạo tài khoản '{tenDangNhap}'.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            var vm = new TaiKhoanFormViewModel
            {
                Id = user.Id,
                Username = user.Username,
                IsActive = user.IsActive,
                IsSuperAdmin = user.IsSuperAdmin,
                Roles = await LoadRolesAsync(user.UserRoles.Select(ur => ur.RoleId).ToList())
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TaiKhoanFormViewModel model, List<int>? roleIds)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == model.Id);

            if (user == null)
            {
                return NotFound();
            }

            var chon = (roleIds ?? new List<int>()).Distinct().ToList();

            string? loi = null;
            if (user.Id == CurrentUserId() && !model.IsActive)
            {
                loi = "Không thể khóa chính tài khoản đang đăng nhập.";
            }
            else if (!string.IsNullOrEmpty(model.Password))
            {
                loi = MatKhauHelper.KiemTraDoManh(model.Password);
            }

            loi ??= await KiemTraVaiTroAsync(chon);

            if (loi == null && await LaSuperAdminCuoiCungAsync(user, model.IsActive, chon))
            {
                loi = "Hệ thống phải luôn còn ít nhất một SuperAdmin đang hoạt động.";
            }

            if (loi != null)
            {
                ModelState.AddModelError(string.Empty, loi);
                model.Username = user.Username;
                model.IsSuperAdmin = user.IsSuperAdmin;
                model.Roles = await LoadRolesAsync(chon);
                return View(model);
            }

            user.IsActive = model.IsActive;

            foreach (var ur in user.UserRoles.Where(ur => !chon.Contains(ur.RoleId)).ToList())
            {
                _context.UserRoles.Remove(ur);
            }

            foreach (var roleId in chon.Where(rid => !user.UserRoles.Any(ur => ur.RoleId == rid)))
            {
                user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });
            }

            if (!string.IsNullOrEmpty(model.Password))
            {
                string salt = MatKhauHelper.GenerateSalt();
                user.PasswordSalt = salt;
                user.PasswordHash = MatKhauHelper.HashPassword(model.Password, salt);
            }

            await _context.SaveChangesAsync();

            TempData["ThongBao"] = $"Đã cập nhật tài khoản '{user.Username}'.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.IsActive = true;
            await _context.SaveChangesAsync();

            TempData["ThongBao"] = $"Đã duyệt tài khoản '{user.Username}'. Vui lòng gán vai trò.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Lock(int id)
        {
            if (id == CurrentUserId())
            {
                TempData["LoiDuyet"] = "Không thể khóa chính tài khoản đang đăng nhập.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            if (await LaSuperAdminCuoiCungAsync(user, false, new List<int>()))
            {
                TempData["LoiDuyet"] = "Hệ thống phải luôn còn ít nhất một SuperAdmin đang hoạt động.";
                return RedirectToAction(nameof(Index));
            }

            user.IsActive = false;
            await _context.SaveChangesAsync();

            TempData["ThongBao"] = $"Đã khóa tài khoản '{user.Username}'.";
            return RedirectToAction(nameof(Index));
        }

        // ===== Hàm hỗ trợ =====

        private int CurrentUserId()
        {
            return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
        }

        private bool LaSuperAdminDangNhap()
        {
            return User.IsInRole(MaSuperAdmin)
                || string.Equals(User.FindFirst("IsSuperAdmin")?.Value, "True", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Chỉ SuperAdmin mới được gán vai trò SuperAdmin.</summary>
        private async Task<string?> KiemTraVaiTroAsync(List<int> roleIds)
        {
            var codes = await _context.Roles
                .Where(r => roleIds.Contains(r.Id))
                .Select(r => r.Code)
                .ToListAsync();

            if (codes.Contains(MaSuperAdmin) && !LaSuperAdminDangNhap())
            {
                return "Chỉ SuperAdmin mới được gán vai trò SuperAdmin.";
            }

            return null;
        }

        /// <summary>
        /// Trả về true nếu thay đổi sẽ làm mất SuperAdmin hoạt động cuối cùng của hệ thống.
        /// </summary>
        private async Task<bool> LaSuperAdminCuoiCungAsync(User user, bool sauKhiSuaActive, List<int> sauKhiSuaRoleIds)
        {
            bool laSuperHienTai = user.IsSuperAdmin || user.UserRoles.Any(ur => ur.Role.Code == MaSuperAdmin);
            if (!laSuperHienTai)
            {
                return false;
            }

            var codesSau = await _context.Roles
                .Where(r => sauKhiSuaRoleIds.Contains(r.Id))
                .Select(r => r.Code)
                .ToListAsync();

            bool laSuperSau = sauKhiSuaActive && (user.IsSuperAdmin || codesSau.Contains(MaSuperAdmin));
            if (laSuperSau)
            {
                return false;
            }

            bool conSuperKhac = await _context.Users.AnyAsync(u =>
                u.Id != user.Id
                && u.IsActive
                && (u.IsSuperAdmin || u.UserRoles.Any(ur => ur.Role.Code == MaSuperAdmin)));

            return !conSuperKhac;
        }

        private async Task<List<RoleCheckItem>> LoadRolesAsync(List<int> chon)
        {
            var roles = await _context.Roles
                .Where(r => r.IsActive)
                .OrderBy(r => r.Id)
                .ToListAsync();

            return roles.Select(r => new RoleCheckItem
            {
                Id = r.Id,
                Code = r.Code,
                Name = r.Name,
                Checked = chon.Contains(r.Id)
            }).ToList();
        }
    }
}
