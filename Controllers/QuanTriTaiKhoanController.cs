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
    /// Quản lý tài khoản: duyệt đăng ký, tạo tài khoản cho nhân viên, cấu hình ẩn/hiện menu chức năng, gán vai trò, khóa và đặt lại mật khẩu.
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
        private readonly IPermissionService _permissionService;

        public QuanTriTaiKhoanController(AppDbContext context, ISuperAdminService superAdmin, IPermissionService permissionService)
        {
            _context = context;
            _superAdmin = superAdmin;
            _permissionService = permissionService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? loc)
        {
            var query = _context.Users
                .Include(u => u.Employee)
                    .ThenInclude(e => e!.Department)
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
                    EmployeeId = u.EmployeeId,
                    EmployeeCode = u.Employee?.EmployeeCode,
                    Username = u.Username,
                    FullName = u.Employee != null ? u.Employee.FullName : null,
                    DepartmentName = u.Employee?.Department?.Name,
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
        public async Task<IActionResult> Create(int? empId)
        {
            var employees = await LoadEmployeeOptionsAsync();
            var defaultFeatures = MenuFeatureDefinitions.GetAllFeatures();

            var vm = new TaiKhoanFormViewModel
            {
                IsActive = true,
                EmployeeId = empId,
                Password = "Admin@123456",
                Roles = await LoadRolesAsync(new List<int>()),
                Employees = employees,
                MenuFeatures = defaultFeatures
            };

            if (empId.HasValue)
            {
                var emp = await _context.Employees.Include(e => e.Department).FirstOrDefaultAsync(e => e.Id == empId.Value);
                if (emp != null)
                {
                    vm.EmployeeFullName = emp.FullName;
                    vm.EmployeeCode = emp.EmployeeCode;
                    vm.DepartmentName = emp.Department?.Name;
                    vm.Username = emp.EmployeeCode.ToLower();
                }
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaiKhoanFormViewModel model, List<int>? roleIds, List<string>? featureCodes)
        {
            string tenDangNhap = (model.Username ?? string.Empty).Trim();
            var chon = (roleIds ?? new List<int>()).Distinct().ToList();

            string? loi = null;
            if (tenDangNhap.Length < 3 || tenDangNhap.Length > 50)
            {
                loi = "Tên đăng nhập từ 3 đến 50 ký tự.";
            }
            else if (await _context.Users.AnyAsync(u => u.Username == tenDangNhap))
            {
                loi = "Tên đăng nhập đã tồn tại trong hệ thống.";
            }
            else
            {
                loi = MatKhauHelper.KiemTraDoManh(model.Password);
            }

            loi ??= await KiemTraVaiTroAsync(chon);

            if (model.EmployeeId.HasValue && await _context.Users.AnyAsync(u => u.EmployeeId == model.EmployeeId.Value))
            {
                loi = "Nhân viên này đã được cấp tài khoản trên hệ thống!";
            }

            if (loi != null)
            {
                ModelState.AddModelError(string.Empty, loi);
                model.Roles = await LoadRolesAsync(chon);
                model.Employees = await LoadEmployeeOptionsAsync();
                model.MenuFeatures = MenuFeatureDefinitions.GetAllFeatures();
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
                MustChangePassword = model.MustChangePassword,
                EmployeeId = model.EmployeeId,
                CreatedAt = VietnamTime.Now
            };

            // Nếu không chọn vai trò nào, mặc định cấp vai trò EMPLOYEE
            if (!chon.Any())
            {
                var empRole = await _context.Roles.FirstOrDefaultAsync(r => r.Code == "EMPLOYEE");
                if (empRole != null) chon.Add(empRole.Id);
            }

            foreach (var roleId in chon)
            {
                user.UserRoles.Add(new UserRole { RoleId = roleId });
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Cấu hình danh sách Menu / Chức năng được phép xem
            if (featureCodes != null && featureCodes.Any())
            {
                await CapNhatQuyenMenuChoUserAsync(user.Id, featureCodes);
            }

            TempData["ThongBao"] = $"Đã tạo tài khoản '{tenDangNhap}' cho nhân viên thành công.";
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
                .Include(u => u.Employee)
                    .ThenInclude(e => e!.Department)
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            var vm = new TaiKhoanFormViewModel
            {
                Id = user.Id,
                EmployeeId = user.EmployeeId,
                EmployeeFullName = user.Employee?.FullName,
                EmployeeCode = user.Employee?.EmployeeCode,
                DepartmentName = user.Employee?.Department?.Name,
                Username = user.Username,
                IsActive = user.IsActive,
                IsSuperAdmin = user.IsSuperAdmin,
                Roles = await LoadRolesAsync(user.UserRoles.Select(ur => ur.RoleId).ToList()),
                Employees = await LoadEmployeeOptionsAsync()
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
                model.Employees = await LoadEmployeeOptionsAsync();
                return View(model);
            }

            bool doiKichHoat = user.IsActive != model.IsActive;
            bool doiVaiTro = !user.UserRoles.Select(ur => ur.RoleId).OrderBy(x => x)
                .SequenceEqual(chon.OrderBy(x => x));

            user.IsActive = model.IsActive;
            user.EmployeeId = model.EmployeeId;

            foreach (var ur in user.UserRoles.Where(ur => !chon.Contains(ur.RoleId)).ToList())
            {
                _context.UserRoles.Remove(ur);
            }

            foreach (var roleId in chon.Where(rid => !user.UserRoles.Any(ur => ur.RoleId == rid)))
            {
                user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });
            }

            if (doiKichHoat || doiVaiTro)
            {
                user.SecurityStamp = Guid.NewGuid();
            }

            await _context.SaveChangesAsync();

            TempData["ThongBao"] = $"Đã cập nhật tài khoản '{user.Username}'.";
            return RedirectToAction(nameof(Index));
        }

        // ════════════════════════════════════════════════════════════════════════════════
        // MÀN HÌNH CẤU HÌNH MENU & ẨN/HIỆN CHỨC NĂNG DÀNH RIÊNG CHO NHÂN VIÊN
        // ════════════════════════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> MenuNhanVien(int id)
        {
            var user = await _context.Users
                .Include(u => u.Employee)
                    .ThenInclude(e => e!.Department)
                .Include(u => u.Employee)
                    .ThenInclude(e => e!.Position)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound();

            var allFeatures = MenuFeatureDefinitions.GetAllFeatures();
            var effectivePerms = (await _permissionService.GetEffectivePermissionsAsync(user.Id))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var f in allFeatures)
            {
                if (user.IsSuperAdmin)
                {
                    f.Checked = true;
                }
                else
                {
                    f.Checked = f.PermissionCodes.Any(code => effectivePerms.Contains(code));
                }
            }

            var vm = new MenuNhanVienViewModel
            {
                UserId = user.Id,
                EmployeeId = user.EmployeeId,
                Username = user.Username,
                FullName = user.Employee?.FullName ?? user.Username,
                EmployeeCode = user.Employee?.EmployeeCode,
                DepartmentName = user.Employee?.Department?.Name,
                PositionName = user.Employee?.Position?.Name,
                IsSuperAdmin = user.IsSuperAdmin,
                Features = allFeatures
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MenuNhanVien(int userId, List<string>? featureCodes)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            if (user.IsSuperAdmin)
            {
                TempData["LoiDuyet"] = "Tài khoản Admin Tổng luôn có toàn quyền và không thể bị giới hạn menu.";
                return RedirectToAction(nameof(Index));
            }

            var selectedCodes = featureCodes ?? new List<string>();
            await CapNhatQuyenMenuChoUserAsync(user.Id, selectedCodes);

            user.SecurityStamp = Guid.NewGuid();
            await _context.SaveChangesAsync();

            TempData["ThongBao"] = $"Đã cập nhật cấu hình menu hiển thị cho nhân viên '{user.Username}'. Khi đăng nhập, nhân viên chỉ xem được các chức năng Admin Tổng đã cấp!";
            return RedirectToAction(nameof(Index));
        }

        private async Task CapNhatQuyenMenuChoUserAsync(int userId, List<string> activeFeatureCodes)
        {
            var allFeatures = MenuFeatureDefinitions.GetAllFeatures();
            var allPerms = await _context.Permissions.ToListAsync();
            var permByCode = allPerms.ToDictionary(p => p.Code.ToLowerInvariant(), p => p);

            var grantedPermCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var deniedPermCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var f in allFeatures)
            {
                if (activeFeatureCodes.Contains(f.Code, StringComparer.OrdinalIgnoreCase))
                {
                    foreach (var c in f.PermissionCodes) grantedPermCodes.Add(c);
                }
                else
                {
                    foreach (var c in f.PermissionCodes) deniedPermCodes.Add(c);
                }
            }

            foreach (var code in grantedPermCodes)
            {
                deniedPermCodes.Remove(code);
            }

            var existingUserPerms = await _context.UserPermissions.Where(up => up.UserId == userId).ToListAsync();

            // 1. Cấp quyền các chức năng được chọn
            foreach (var code in grantedPermCodes)
            {
                if (permByCode.TryGetValue(code.ToLowerInvariant(), out var p))
                {
                    var up = existingUserPerms.FirstOrDefault(x => x.PermissionId == p.Id);
                    if (up != null)
                    {
                        up.IsGranted = true;
                        up.AssignedAt = VietnamTime.Now;
                    }
                    else
                    {
                        _context.UserPermissions.Add(new UserPermission
                        {
                            UserId = userId,
                            PermissionId = p.Id,
                            IsGranted = true,
                            AssignedAt = VietnamTime.Now
                        });
                    }
                }
            }

            // 2. Chặn quyền các chức năng bị ẩn
            foreach (var code in deniedPermCodes)
            {
                if (permByCode.TryGetValue(code.ToLowerInvariant(), out var p))
                {
                    var up = existingUserPerms.FirstOrDefault(x => x.PermissionId == p.Id);
                    if (up != null)
                    {
                        up.IsGranted = false;
                        up.AssignedAt = VietnamTime.Now;
                    }
                    else
                    {
                        _context.UserPermissions.Add(new UserPermission
                        {
                            UserId = userId,
                            PermissionId = p.Id,
                            IsGranted = false,
                            AssignedAt = VietnamTime.Now
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task<List<EmployeeSelectOption>> LoadEmployeeOptionsAsync()
        {
            var existingEmpUserIds = (await _context.Users
                .Where(u => u.EmployeeId != null)
                .Select(u => u.EmployeeId!.Value)
                .ToListAsync())
                .ToHashSet();

            return await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Position)
                .Where(e => e.Status != "Resigned")
                .OrderBy(e => e.EmployeeCode)
                .Select(e => new EmployeeSelectOption
                {
                    Id = e.Id,
                    EmployeeCode = e.EmployeeCode,
                    FullName = e.FullName,
                    DepartmentName = e.Department.Name,
                    PositionName = e.Position.Name,
                    Email = e.Email,
                    HasAccount = existingEmpUserIds.Contains(e.Id)
                }).ToListAsync();
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

            TempData["ThongBao"] = $"Đã duyệt và kích hoạt tài khoản '{user.Username}'.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Lock(int id)
        {
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

            if (user.Id == CurrentUserId())
            {
                TempData["LoiDuyet"] = "Không thể tự khóa tài khoản của chính mình.";
                return RedirectToAction(nameof(Index));
            }

            if (await LaSuperAdminCuoiCungAsync(user, false, user.UserRoles.Select(ur => ur.RoleId).ToList()))
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

        [HttpGet]
        [KiemTraQuyen("auth.user.resetpassword")]
        public async Task<IActionResult> ResetPassword(int id)
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

            ViewBag.Username = user.Username;
            ViewBag.UserId = user.Id;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("auth.user.resetpassword")]
        public async Task<IActionResult> ResetPassword(int id, string? newPassword)
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

            string? loi = MatKhauHelper.KiemTraDoManh(newPassword);
            if (loi != null)
            {
                ModelState.AddModelError(string.Empty, loi);
                ViewBag.Username = user.Username;
                ViewBag.UserId = user.Id;
                return View();
            }

            string salt = MatKhauHelper.GenerateSalt();
            user.PasswordSalt = salt;
            user.PasswordHash = MatKhauHelper.HashPassword(newPassword!, salt);
            user.MustChangePassword = true;
            user.SecurityStamp = Guid.NewGuid();

            await _context.SaveChangesAsync();

            TempData["ThongBao"] = $"Đã đặt lại mật khẩu cho tài khoản '{user.Username}'. Yêu cầu đổi mật khẩu ở lần đăng nhập tới.";
            return RedirectToAction(nameof(Index));
        }

        private int CurrentUserId()
        {
            return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
        }

        private async Task<bool> LaTaiKhoanBiBaoVeAsync(int targetId)
        {
            int nguoiThaoTac = CurrentUserId();
            if (nguoiThaoTac > 0 && await _superAdmin.LaSuperAdminAsync(nguoiThaoTac))
            {
                return false;
            }

            return await _superAdmin.LaTaiKhoanQuanTriTongAsync(targetId);
        }

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
