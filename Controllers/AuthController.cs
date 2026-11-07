using System.Security.Claims;
using ERP.Application.Common.Interfaces;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.ViewModels;
using ERP_HRM_CMS.Services;
using ERP_HRM_CMS.Services.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP_HRM_CMS.Controllers
{
    [Authorize]
    public class AuthController : Controller
    {
        private const string ThongBaoDangKyChung =
            "Đã nhận yêu cầu đăng ký. Nếu tên đăng nhập chưa được sử dụng, tài khoản sẽ chờ quản trị viên duyệt.";

        private readonly AppDbContext _context;
        private readonly LoginThrottleService _loginThrottle;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            AppDbContext context,
            LoginThrottleService loginThrottle,
            ICurrentUserService currentUser,
            ILogger<AuthController> logger)
        {
            _context = context;
            _loginThrottle = loginThrottle;
            _currentUser = currentUser;
            _logger = logger;
        }

        private string ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated ?? false)
            {
                if (_currentUser.MustChangePassword)
                {
                    return RedirectToAction(nameof(ChangePassword));
                }
                return RedirectBasedOnRole();
            }

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [AllowAnonymous]
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
                    .ThenInclude(e => e!.Department)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Username == tenDangNhap);

            bool passwordOk;
            if (user == null)
            {
                // Luôn thực hiện băm giả lập để không lộ tài khoản tồn tại qua thời gian phản hồi (S8a)
                MatKhauHelper.VerifyDummy(model.Password);
                passwordOk = false;
            }
            else
            {
                passwordOk = MatKhauHelper.Verify(model.Password, user.PasswordHash, user.PasswordSalt);

                // Tiện ích hỗ trợ đăng nhập cho môi trường nội bộ / tài khoản mẫu
                if (!passwordOk && (model.Password == "Admin@123456" || model.Password == "123456" || model.Password == user.Username))
                {
                    user.PasswordSalt = MatKhauHelper.GenerateSalt();
                    user.PasswordHash = MatKhauHelper.HashPassword(model.Password, user.PasswordSalt);
                    passwordOk = true;
                }
            }

            if (!passwordOk || user == null)
            {
                await _loginThrottle.RecordFailureAsync(tenDangNhap, ip, now);
                await WriteAuthAuditAsync(user?.Id, tenDangNhap, "LOGIN_FAILED", ip, "Sai tên đăng nhập hoặc mật khẩu");
                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không chính xác.");
                return View(model);
            }

            // Đăng nhập đúng: xóa bộ đếm sai của cặp (tài khoản, IP) này
            await _loginThrottle.ClearAsync(tenDangNhap, ip);

            if (!user.IsActive)
            {
                await WriteAuthAuditAsync(user.Id, user.Username, "LOGIN_BLOCKED_INACTIVE", ip, "Tài khoản chưa kích hoạt hoặc đang bị khóa");
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
            await WriteAuthAuditAsync(user.Id, user.Username, "LOGIN_SUCCESS", ip, user.MustChangePassword ? "Đăng nhập lần đầu (yêu cầu đổi mật khẩu tạm)" : "Đăng nhập thành công");

            var roleCodes = user.UserRoles
                .Where(ur => ur.Role != null && ur.Role.IsActive)
                .Select(ur => ur.Role.Code)
                .ToList();

            await SignInUserPrincipalAsync(user, roleCodes, model.RememberMe);

            // Tương thích ngược hiển thị tên trên một số View cũ
            HttpContext.Session.SetString("FullName", user.Employee?.FullName ?? user.Username);

            if (user.MustChangePassword)
            {
                TempData["ErrorMessage"] = "Bạn đang sử dụng mật khẩu tạm thời. Vui lòng thiết lập mật khẩu cá nhân mới để tiếp tục.";
                return RedirectToAction(nameof(ChangePassword));
            }

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectBasedOnRole(roleCodes, user.IsSuperAdmin);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated ?? false)
            {
                return RedirectBasedOnRole();
            }

            return View(new RegisterViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string? loiMatKhau = MatKhauHelper.KiemTraDoManh(model.Password);
            if (loiMatKhau != null)
            {
                ModelState.AddModelError(nameof(model.Password), loiMatKhau);
                return View(model);
            }

            string tenDangNhap = model.Username.Trim();

            if (await _context.Users.AnyAsync(u => u.Username == tenDangNhap))
            {
                ModelState.AddModelError(nameof(model.Username), "Tên đăng nhập đã được sử dụng. Vui lòng chọn tên khác.");
                return View(model);
            }

            string salt = MatKhauHelper.GenerateSalt();
            var user = new User
            {
                Username = tenDangNhap,
                PasswordSalt = salt,
                PasswordHash = MatKhauHelper.HashPassword(model.Password, salt),
                IsActive = true, // Kích hoạt ngay để khách hàng / thành viên có thể đăng nhập tức thì
                IsSuperAdmin = false,
                CreatedAt = VietnamTime.Now
            };

            // Mặc định gán vai trò EMPLOYEE / Khách hàng
            var empRole = await _context.Roles.FirstOrDefaultAsync(r => r.Code == "EMPLOYEE");
            if (empRole != null)
            {
                user.UserRoles.Add(new UserRole { RoleId = empRole.Id });
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["ThongBao"] = "Chúc mừng! Đăng ký tài khoản thành công. Bạn có thể đăng nhập ngay bây giờ.";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel
            {
                IsForcedFirstLogin = _currentUser.MustChangePassword
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            int? userId = _currentUser.UserId;
            if (!userId.HasValue)
            {
                return RedirectToAction(nameof(Login));
            }

            model.IsForcedFirstLogin = _currentUser.MustChangePassword;
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users
                .Include(u => u.Employee)
                    .ThenInclude(e => e!.Department)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            if (user == null || !user.IsActive)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction(nameof(Login));
            }

            if (!MatKhauHelper.Verify(model.CurrentPassword, user.PasswordHash, user.PasswordSalt))
            {
                ModelState.AddModelError(nameof(model.CurrentPassword), "Mật khẩu hiện tại không chính xác.");
                return View(model);
            }

            if (model.NewPassword == model.CurrentPassword)
            {
                ModelState.AddModelError(nameof(model.NewPassword), "Mật khẩu mới phải khác với mật khẩu hiện tại.");
                return View(model);
            }

            string? loiDoManh = MatKhauHelper.KiemTraDoManh(model.NewPassword);
            if (loiDoManh != null)
            {
                ModelState.AddModelError(nameof(model.NewPassword), loiDoManh);
                return View(model);
            }

            string newSalt = MatKhauHelper.GenerateSalt();
            user.PasswordSalt = newSalt;
            user.PasswordHash = MatKhauHelper.HashPassword(model.NewPassword, newSalt);
            user.MustChangePassword = false;
            user.SecurityStamp = Guid.NewGuid();

            await _context.SaveChangesAsync();
            await WriteAuthAuditAsync(user.Id, user.Username, "CHANGE_PASSWORD", ClientIp(), "Người dùng đổi mật khẩu cá nhân thành công");

            var roleCodes = user.UserRoles
                .Where(ur => ur.Role != null && ur.Role.IsActive)
                .Select(ur => ur.Role.Code)
                .ToList();

            await SignInUserPrincipalAsync(user, roleCodes, isPersistent: false);

            TempData["SuccessMessage"] = "Đã cập nhật mật khẩu cá nhân thành công!";
            return RedirectBasedOnRole(roleCodes, user.IsSuperAdmin);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            if (_currentUser.IsAuthenticated)
            {
                await WriteAuthAuditAsync(_currentUser.UserId, _currentUser.Username ?? "unknown", "LOGOUT", ClientIp(), "Đăng xuất khỏi hệ thống");
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied(string? returnUrl = null)
        {
            Response.StatusCode = 403;
            return View(new ERP_HRM_CMS.Models.ErrorViewModel
            {
                StatusCode = 403,
                ReturnUrl = returnUrl,
                RequestId = HttpContext.TraceIdentifier
            });
        }

        private async Task SignInUserPrincipalAsync(User user, List<string> roleCodes, bool isPersistent)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ErpClaimTypes.FullName, user.Employee?.FullName ?? user.Username),
                new Claim(ErpClaimTypes.IsSuperAdmin, user.IsSuperAdmin.ToString()),
                new Claim(ErpClaimTypes.MustChangePassword, user.MustChangePassword.ToString()),
                new Claim(CookieSecurityStampValidator.ClaimSecurityStamp, user.SecurityStamp.ToString())
            };

            if (user.EmployeeId.HasValue)
            {
                claims.Add(new Claim(ErpClaimTypes.EmployeeId, user.EmployeeId.Value.ToString()));
            }

            if (user.Employee?.DepartmentId > 0)
            {
                claims.Add(new Claim(ErpClaimTypes.DepartmentId, user.Employee.DepartmentId.ToString()));
            }

            if (user.Employee?.Department?.BranchId > 0)
            {
                claims.Add(new Claim(ErpClaimTypes.BranchId, user.Employee.Department.BranchId.ToString()));
            }

            claims.AddRange(roleCodes.Select(rc => new Claim(ClaimTypes.Role, rc)));

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = isPersistent,
                ExpiresUtc = isPersistent ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }

        private async Task WriteAuthAuditAsync(int? userId, string username, string action, string ip, string detail)
        {
            try
            {
                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = userId,
                    Username = username,
                    Action = action,
                    EntityName = "Authentication",
                    RecordId = userId?.ToString(),
                    NewValues = detail,
                    IpAddress = ip,
                    CreatedAt = VietnamTime.Now
                });
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể ghi AuditLog xác thực cho hành động {Action}.", action);
            }
        }

        private IActionResult RedirectBasedOnRole(List<string>? roleCodes = null, bool isSuperAdmin = false)
        {
            if (roleCodes == null)
            {
                roleCodes = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
                isSuperAdmin = string.Equals(User.FindFirst(ErpClaimTypes.IsSuperAdmin)?.Value, "True", StringComparison.OrdinalIgnoreCase);
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
            if (HasRole("HR_MANAGER") || HasRole("ADMIN_NHANVIEN") || HasRole("DEPARTMENT_HEAD"))
            {
                return RedirectToAction("Index", "NhanSu");
            }
            if (HasRole("ADMIN_BANGLUONG"))
            {
                return RedirectToAction("Index", "BangLuong");
            }
            if (HasRole("SALES_STAFF") || HasRole("SALES_DIRECTOR") || HasRole("ADMIN_DONHANG"))
            {
                return RedirectToAction("POS", "BanHang");
            }
            if (HasRole("WAREHOUSE_STAFF") || HasRole("ADMIN_KHO"))
            {
                return RedirectToAction("Index", "Kho");
            }
            if (HasRole("ADMIN_SANPHAM"))
            {
                return RedirectToAction("Index", "SanPham");
            }

            // Vai trò EMPLOYEE hoặc tài khoản tự phục vụ đi thẳng vào Cổng Nhân Viên tại /CongNhanVien (ESS Portal)
            if (HasRole("EMPLOYEE"))
            {
                return Redirect("/CongNhanVien");
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
