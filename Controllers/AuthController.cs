using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.ViewModels;
using ERP_HRM_CMS.Services;

namespace ERP_HRM_CMS.Controllers
{
    // Toàn bộ action của AuthController là công khai (đăng nhập, đăng ký, đăng xuất).
    [AllowAnonymous]
    public class AuthController : Controller
    {
        private readonly AppDbContext _context;
        private readonly LoginThrottleService _loginThrottle;

        public AuthController(AppDbContext context, LoginThrottleService loginThrottle)
        {
            _context = context;
            _loginThrottle = loginThrottle;
        }

        private string ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated ?? false)
            {
                return RedirectBasedOnRole();
            }

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string tenDangNhap = model.Username.Trim();
            string ip = ClientIp();
            var now = DateTime.UtcNow;

            var conLai = await _loginThrottle.GetRemainingLockoutAsync(tenDangNhap, ip, now);
            if (conLai is TimeSpan remaining)
            {
                ModelState.AddModelError(string.Empty,
                    $"Đăng nhập sai quá nhiều lần. Vui lòng thử lại sau khoảng {Math.Ceiling(remaining.TotalMinutes)} phút.");
                return View(model);
            }

            var user = await _context.Users
                .Include(u => u.Employee)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Username == tenDangNhap);

            if (user == null || !MatKhauHelper.Verify(model.Password, user.PasswordHash, user.PasswordSalt))
            {
                await _loginThrottle.RecordFailureAsync(tenDangNhap, ip, now);
                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không chính xác.");
                return View(model);
            }

            // Đăng nhập đúng: xóa bộ đếm sai của cặp (tài khoản, IP) này
            await _loginThrottle.ClearAsync(tenDangNhap, ip);

            if (!user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "Tài khoản đang chờ duyệt hoặc đã bị khóa. Vui lòng liên hệ quản trị viên.");
                return View(model);
            }

            // Nâng cấp mật khẩu cũ (SHA-256) sang PBKDF2 sau khi xác thực thành công
            if (MatKhauHelper.IsLegacyHash(user.PasswordHash))
            {
                user.PasswordSalt = MatKhauHelper.GenerateSalt();
                user.PasswordHash = MatKhauHelper.HashPassword(model.Password, user.PasswordSalt);
            }

            user.LastLoginAt = VietnamTime.Now;
            await _context.SaveChangesAsync();

            var roleCodes = user.UserRoles.Select(ur => ur.Role.Code).ToList();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("FullName", user.Employee?.FullName ?? user.Username),
                new Claim("IsSuperAdmin", user.IsSuperAdmin.ToString())
            };
            claims.AddRange(roleCodes.Select(rc => new Claim(ClaimTypes.Role, rc)));

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("FullName", user.Employee?.FullName ?? user.Username);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectBasedOnRole(roleCodes, user.IsSuperAdmin);
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated ?? false)
            {
                return RedirectBasedOnRole();
            }

            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string tenDangNhap = model.Username.Trim();

            if (await _context.Users.AnyAsync(u => u.Username == tenDangNhap))
            {
                ModelState.AddModelError(nameof(model.Username), "Tên đăng nhập đã tồn tại.");
                return View(model);
            }

            string? loiMatKhau = MatKhauHelper.KiemTraDoManh(model.Password);
            if (loiMatKhau != null)
            {
                ModelState.AddModelError(nameof(model.Password), loiMatKhau);
                return View(model);
            }

            // Tài khoản đăng ký mới chưa có vai trò và chưa được kích hoạt: chờ quản trị viên duyệt
            string salt = MatKhauHelper.GenerateSalt();
            var user = new User
            {
                Username = tenDangNhap,
                PasswordSalt = salt,
                PasswordHash = MatKhauHelper.HashPassword(model.Password, salt),
                IsActive = false,
                IsSuperAdmin = false,
                CreatedAt = VietnamTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["ThongBao"] = "Đăng ký thành công. Tài khoản đang chờ quản trị viên duyệt.";
            return RedirectToAction(nameof(Login));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Login));
        }

        private IActionResult RedirectBasedOnRole(List<string>? roleCodes = null, bool isSuperAdmin = false)
        {
            if (roleCodes == null)
            {
                roleCodes = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
                isSuperAdmin = string.Equals(User.FindFirst("IsSuperAdmin")?.Value, "True", StringComparison.OrdinalIgnoreCase);
            }

            var roles = roleCodes;
            bool HasRole(string code) => roles.Any(r => string.Equals(r, code, StringComparison.OrdinalIgnoreCase));

            if (isSuperAdmin || HasRole("SUPERADMIN"))
            {
                return RedirectToAction("MaTranPhanQuyen", "PhanQuyen");
            }
            if (HasRole("ADMIN_MENU"))
            {
                return RedirectToAction("Index", "Menu");
            }
            if (HasRole("ADMIN_CONTENT"))
            {
                return RedirectToAction("Index", "Content");
            }
            if (HasRole("HR_MANAGER"))
            {
                return RedirectToAction("Index", "NhanSu");
            }
            if (HasRole("SALES_STAFF") || HasRole("SALES_DIRECTOR"))
            {
                return RedirectToAction("POS", "BanHang");
            }
            if (HasRole("WAREHOUSE_STAFF"))
            {
                return RedirectToAction("Index", "Kho");
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
