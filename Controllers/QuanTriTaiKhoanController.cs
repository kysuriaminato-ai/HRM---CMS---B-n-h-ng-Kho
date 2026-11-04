using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Filters;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.ViewModels;
using ERP_HRM_CMS.Services.Security;

namespace ERP_HRM_CMS.Controllers
{
    /// <summary>
    /// Quản lý tài khoản: duyệt đăng ký, tạo tài khoản, gán vai trò, khóa và đặt lại mật khẩu.
    /// Quyền "auth.user.manage" để vào trang; "auth.user.resetpassword" để đặt lại mật khẩu.
    /// Tài khoản quản trị tổng (IsSuperAdmin hoặc vai trò SUPERADMIN) chỉ SuperAdmin được thao tác.
    /// </summary>
    [Authorize]
    [KiemTraQuyen("auth.user.manage")]
    public class QuanTriTaiKhoanController : Controller
    {
        private const string ThongBaoBaoVe = "Chỉ SuperAdmin mới được thao tác trên tài khoản quản trị tổng.";
        private readonly AppDbContext _context;
        private readonly ISuperAdminService _superAdmin;

        public QuanTriTaiKhoanController(AppDbContext context, ISuperAdminService superAdmin)
        {
            _context = context;
            _superAdmin = superAdmin;
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
                CreatedAt = VietnamTime.Now
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
            if (await LaTaiKhoanBiBaoVeAsync(id))
            {
                TempData["LoiDuyet"] = ThongBaoBaoVe;
                return RedirectToAction(nameof(Index));
            }

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

        /// <summary>Sửa trạng thái và vai trò. Mật khẩu đổi ở action ResetPassword riêng.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TaiKhoanFormViewModel model, List<int>? roleIds)
        {
            if (await LaTaiKhoanBiBaoVeAsync(model.Id))
            {
                TempData["LoiDuyet"] = ThongBaoBaoVe;
                return RedirectToAction(nameof(Index));
            }

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

            // Tính trước khi thay đổi để biết có cần thu hồi phiên không
            bool doiKichHoat = user.IsActive != model.IsActive;
            bool doiVaiTro = !user.UserRoles.Select(ur => ur.RoleId).OrderBy(x => x)
                .SequenceEqual(chon.OrderBy(x => x));

            user.IsActive = model.IsActive;

            foreach (var ur in user.UserRoles.Where(ur => !chon.Contains(ur.RoleId)).ToList())
            {
                _context.UserRoles.Remove(ur);
            }

            foreach (var roleId in chon.Where(rid => !user.UserRoles.Any(ur => ur.RoleId == rid)))
            {
                user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });
            }

            // Đổi trạng thái hoặc vai trò: thu hồi mọi phiên đang đăng nhập của tài khoản
            if (doiKichHoat || doiVaiTro)
            {
                user.SecurityStamp = Guid.NewGuid();
            }

            await _context.SaveChangesAsync();

            TempData["ThongBao"] = $"Đã cập nhật tài khoản '{user.Username}'.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            if (await LaTaiKhoanBiBaoVeAsync(id))
            {
                TempData["LoiDuyet"] = ThongBaoBaoVe;
                return RedirectToAction(nameof(Index));
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.IsActive = true;
            user.SecurityStamp = Guid.NewGuid();
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

            if (await LaTaiKhoanBiBaoVeAsync(id))
            {
                TempData["LoiDuyet"] = ThongBaoBaoVe;
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
            user.SecurityStamp = Guid.NewGuid();
            await _context.SaveChangesAsync();

            TempData["ThongBao"] = $"Đã khóa tài khoản '{user.Username}'.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>Đặt lại mật khẩu. Quyền riêng "auth.user.resetpassword"; ghi AuditLog; thu hồi mọi phiên cũ.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("auth.user.resetpassword")]
        public async Task<IActionResult> ResetPassword(int id, string? matKhauMoi)
        {
            if (await LaTaiKhoanBiBaoVeAsync(id))
            {
                TempData["LoiDuyet"] = ThongBaoBaoVe;
                return RedirectToAction(nameof(Index));
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            string? loi = MatKhauHelper.KiemTraDoManh(matKhauMoi);
            if (loi != null)
            {
                TempData["LoiDuyet"] = loi;
                return RedirectToAction(nameof(Edit), new { id });
            }

            string salt = MatKhauHelper.GenerateSalt();
            user.PasswordSalt = salt;
            user.PasswordHash = MatKhauHelper.HashPassword(matKhauMoi!, salt);
            user.SecurityStamp = Guid.NewGuid();

            // Ghi nhật ký riêng cho thao tác nhạy cảm (không ghi mật khẩu vào nhật ký)
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = CurrentUserId(),
                Username = User.Identity?.Name,
                Action = "RESET_PASSWORD",
                EntityName = "User",
                RecordId = user.Id.ToString(),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                CreatedAt = VietnamTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["ThongBao"] = $"Đã đặt lại mật khẩu cho '{user.Username}'. Các phiên đăng nhập cũ của tài khoản này đã bị thu hồi.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        // ===== Hàm hỗ trợ =====

        private int CurrentUserId()
        {
            return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
        }

        /// <summary>Tài khoản quản trị tổng chỉ SuperAdmin được thao tác (kiểm tra từ database).</summary>
        private async Task<bool> LaTaiKhoanBiBaoVeAsync(int targetId)
        {
            int nguoiThaoTac = CurrentUserId();
            if (nguoiThaoTac > 0 && await _superAdmin.LaSuperAdminAsync(nguoiThaoTac))
            {
                return false;
            }

            return await _superAdmin.LaTaiKhoanQuanTriTongAsync(targetId);
        }

        /// <summary>Chỉ SuperAdmin mới được gán vai trò SuperAdmin (kiểm tra từ database).</summary>
        private async Task<string?> KiemTraVaiTroAsync(List<int> roleIds)
        {
            var codes = await _context.Roles
                .Where(r => roleIds.Contains(r.Id))
                .Select(r => r.Code)
                .ToListAsync();

            if (codes.Any(c => string.Equals(c, SuperAdminService.MaVaiTroSuperAdmin, StringComparison.OrdinalIgnoreCase))
                && !await _superAdmin.LaSuperAdminAsync(CurrentUserId()))
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
            bool laSuperHienTai = user.IsSuperAdmin || user.UserRoles.Any(ur => ur.Role.Code == SuperAdminService.MaVaiTroSuperAdmin);
            if (!laSuperHienTai)
            {
                return false;
            }

            var codesSau = await _context.Roles
                .Where(r => sauKhiSuaRoleIds.Contains(r.Id))
                .Select(r => r.Code)
                .ToListAsync();

            bool laSuperSau = sauKhiSuaActive && (user.IsSuperAdmin || codesSau.Contains(SuperAdminService.MaVaiTroSuperAdmin));
            if (laSuperSau)
            {
                return false;
            }

            bool conSuperKhac = await _context.Users.AnyAsync(u =>
                u.Id != user.Id
                && u.IsActive
                && (u.IsSuperAdmin || u.UserRoles.Any(ur => ur.Role.Code == SuperAdminService.MaVaiTroSuperAdmin)));

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
