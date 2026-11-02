using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.ViewModels;

namespace ERP_HRM_CMS.Controllers
{
    public class AuthController : Controller
    {
        private readonly AppDbContext _context;

        // Khóa tạm thời khi đăng nhập sai nhiều lần theo tên đăng nhập.
        // Lưu trong bộ nhớ: đủ cho một máy chủ; khi khởi động lại thì bộ đếm bị xóa.
        private const int MaxFailedLogins = 5;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
        private const int MaxTrackedUsernames = 10_000;
        private static readonly ConcurrentDictionary<string, LoginAttemptState> _loginAttempts =
            new(StringComparer.OrdinalIgnoreCase);

        private sealed class LoginAttemptState
        {
            public int Failures;
            public DateTime LockedUntilUtc;
        }

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        private static bool IsLockedOut(string username, out TimeSpan remaining)
        {
            remaining = TimeSpan.Zero;
            if (_loginAttempts.TryGetValue(username, out var state) && state.LockedUntilUtc > DateTime.UtcNow)
            {
                remaining = state.LockedUntilUtc - DateTime.UtcNow;
                return true;
            }
            return false;
        }

        private static void RecordFailedLogin(string username)
        {
            // Tránh phình bộ nhớ khi kẻ tấn công thử hàng loạt tên đăng nhập ngẫu nhiên
            if (_loginAttempts.Count > MaxTrackedUsernames)
            {
                _loginAttempts.Clear();
            }

            var state = _loginAttempts.GetOrAdd(username, _ => new LoginAttemptState());
            lock (state)
            {
                state.Failures++;
                if (state.Failures >= MaxFailedLogins)
                {
                    state.LockedUntilUtc = DateTime.UtcNow.Add(LockoutDuration);
                    state.Failures = 0;
                }
            }
        }

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

            if (IsLockedOut(tenDangNhap, out var conLai))
            {
                ModelState.AddModelError(string.Empty,
                    $"Đăng nhập sai quá nhiều lần. Vui lòng thử lại sau khoảng {Math.Ceiling(conLai.TotalMinutes)} phút.");
                return View(model);
            }

            var user = await _context.Users
                .Include(u => u.Employee)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Username == tenDangNhap);

            if (user == null || !MatKhauHelper.Verify(model.Password, user.PasswordHash, user.PasswordSalt))
            {
                RecordFailedLogin(tenDangNhap);
                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không chính xác.");
                return View(model);
            }

            // Đăng nhập đúng: xóa bộ đếm sai của tài khoản này
            _loginAttempts.TryRemove(tenDangNhap, out _);

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

            user.LastLoginAt = DateTime.Now;
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
                CreatedAt = DateTime.Now
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
            if (HasRole("HR_MANAGER") || HasRole("QTVP"))
            {
                return RedirectToAction("Index", "NhanSu");
            }
            if (HasRole("SALES_STAFF") || HasRole("SalesStaff") || HasRole("SaleDirector"))
            {
                return RedirectToAction("POS", "BanHang");
            }
            if (HasRole("WAREHOUSE_STAFF") || HasRole("WarehouseKeeper"))
            {
                return RedirectToAction("Index", "Kho");
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
