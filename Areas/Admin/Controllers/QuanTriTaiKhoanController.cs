using System.Globalization;
using System.Security.Claims;
using System.Text;
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
    [Area("Admin")]
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
                MustChangePassword = true,
                EmployeeId = empId,
                Password = MatKhauHelper.GenerateTemporaryPassword(12),
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
        public async Task<IActionResult> Create(TaiKhoanFormViewModel model, List<int>? roleIds, List<string>? featureCodes, string? vaiTroKhac)
        {
            string tenDangNhap = (model.Username ?? string.Empty).Trim();
            var chon = (roleIds ?? new List<int>()).Distinct().ToList();

            if (string.IsNullOrWhiteSpace(model.Password))
            {
                model.Password = MatKhauHelper.GenerateTemporaryPassword(12);
            }
            model.MustChangePassword = true;

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

            var hasher = new LegacyCompatiblePasswordHasher();
            string salt = MatKhauHelper.GenerateSalt();
            var user = new User
            {
                Username = tenDangNhap,
                PasswordSalt = salt,
                IsActive = model.IsActive,
                IsSuperAdmin = false,
                MustChangePassword = true,
                EmployeeId = model.EmployeeId,
                CreatedAt = VietnamTime.Now
            };
            user.PasswordHash = hasher.HashPassword(user, model.Password!);

            int? idVaiTroKhac = await TaoVaiTroTuNhapAsync(vaiTroKhac);
            if (idVaiTroKhac.HasValue && !chon.Contains(idVaiTroKhac.Value))
            {
                chon.Add(idVaiTroKhac.Value);
            }

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

            // Mật khẩu tạm được đưa vào TempData để trang Index hiển thị trong khung
            // riêng có nút sao chép, thay vì nhét lẫn vào câu thông báo.
            TempData["GeneratedUsername"] = tenDangNhap;
            TempData["GeneratedPassword"] = model.Password;
            TempData["GeneratedPasswordContext"] = "Tài khoản vừa được tạo";
            TempData["ThongBao"] = $"Đã tạo tài khoản '{tenDangNhap}' thành công.";
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
        public async Task<IActionResult> Edit(TaiKhoanFormViewModel model, List<int>? roleIds, string? vaiTroKhac)
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

            // Admin tổng là tài khoản kỹ thuật của hệ thống, không phải người trong công ty,
            // nên không được gắn hồ sơ nhân viên. Admin con và nhân sự thì ngược lại.
            if (loi == null && user.IsSuperAdmin && model.EmployeeId.HasValue)
            {
                loi = "Tài khoản quản trị tổng là tài khoản hệ thống nên không gắn hồ sơ nhân viên. "
                    + "Nếu đây là nhân sự của công ty, hãy gỡ quyền quản trị tổng trước rồi cấp vai trò quản lý mảng cho họ.";
            }

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

            int? idVaiTroKhacSua = await TaoVaiTroTuNhapAsync(vaiTroKhac);
            if (idVaiTroKhacSua.HasValue && !chon.Contains(idVaiTroKhacSua.Value))
            {
                chon.Add(idVaiTroKhacSua.Value);
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

            TempData["GeneratedUsername"] = user.Username;
            TempData["GeneratedPassword"] = newPassword;
            TempData["GeneratedPasswordContext"] = "Mật khẩu vừa được đặt lại";
            TempData["ThongBao"] = $"Đã đặt lại mật khẩu cho tài khoản '{user.Username}'.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Gỡ tư cách quản trị tổng của một tài khoản: tắt cờ IsSuperAdmin và bỏ vai trò
        /// SUPERADMIN. Chỉ SuperAdmin được thao tác, không gỡ được chính mình, và hệ thống
        /// phải luôn còn ít nhất một quản trị tổng đang hoạt động.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GoAdminTong(int id)
        {
            if (!await _superAdmin.LaSuperAdminAsync(CurrentUserId()))
            {
                TempData["LoiDuyet"] = ThongBaoBaoVe;
                return RedirectToAction(nameof(Index));
            }

            if (id == CurrentUserId())
            {
                TempData["LoiDuyet"] = "Không thể tự gỡ quyền quản trị tổng của chính mình.";
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

            bool conQuanTriTongKhac = await _context.Users.AnyAsync(u =>
                u.Id != user.Id
                && u.IsActive
                && (u.IsSuperAdmin || u.UserRoles.Any(ur => ur.Role.Code == SuperAdminService.MaVaiTroSuperAdmin)));

            if (!conQuanTriTongKhac)
            {
                TempData["LoiDuyet"] = "Hệ thống phải luôn còn ít nhất một quản trị tổng đang hoạt động.";
                return RedirectToAction(nameof(Index));
            }

            user.IsSuperAdmin = false;
            foreach (var ur in user.UserRoles
                         .Where(ur => ur.Role.Code == SuperAdminService.MaVaiTroSuperAdmin)
                         .ToList())
            {
                _context.UserRoles.Remove(ur);
            }

            // Thu hồi mọi phiên đăng nhập cũ để quyền mới có hiệu lực ngay.
            user.SecurityStamp = Guid.NewGuid();
            await _context.SaveChangesAsync();

            TempData["ThongBao"] = $"Đã gỡ quyền quản trị tổng của tài khoản '{user.Username}'. " +
                                   "Tài khoản vẫn đăng nhập được với các vai trò còn lại.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Cấp lại tư cách quản trị tổng. Có cặp với GoAdminTong để thao tác gỡ không
        /// trở thành đường một chiều.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapAdminTong(int id)
        {
            if (!await _superAdmin.LaSuperAdminAsync(CurrentUserId()))
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

            if (user.EmployeeId.HasValue)
            {
                TempData["LoiDuyet"] = $"Tài khoản '{user.Username}' đang gắn hồ sơ nhân viên nên không thể làm quản trị tổng. "
                                     + "Quản trị tổng là tài khoản hệ thống, không thuộc biên chế công ty. "
                                     + "Hãy gỡ liên kết nhân viên ở màn hình sửa tài khoản trước.";
                return RedirectToAction(nameof(Index));
            }

            user.IsSuperAdmin = true;
            user.IsActive = true;

            var vaiTroSuper = await _context.Roles
                .FirstOrDefaultAsync(r => r.Code == SuperAdminService.MaVaiTroSuperAdmin);
            if (vaiTroSuper != null && !user.UserRoles.Any(ur => ur.RoleId == vaiTroSuper.Id))
            {
                user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = vaiTroSuper.Id });
            }

            user.SecurityStamp = Guid.NewGuid();
            await _context.SaveChangesAsync();

            TempData["ThongBao"] = $"Đã cấp quyền quản trị tổng cho tài khoản '{user.Username}'.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Tạo vai trò từ ô "Khác" người dùng tự điền. Nếu đã có vai trò cùng tên hoặc cùng
        /// mã thì dùng lại thay vì tạo trùng. Trả về Id vai trò, hoặc null nếu bỏ trống.
        /// </summary>
        private async Task<int?> TaoVaiTroTuNhapAsync(string? tenVaiTro)
        {
            string ten = (tenVaiTro ?? string.Empty).Trim();
            if (ten.Length == 0)
            {
                return null;
            }

            if (ten.Length > 150)
            {
                ten = ten.Substring(0, 150);
            }

            var daCo = await _context.Roles.FirstOrDefaultAsync(r => r.Name == ten);
            if (daCo != null)
            {
                if (!daCo.IsActive)
                {
                    daCo.IsActive = true;
                    await _context.SaveChangesAsync();
                }
                return daCo.Id;
            }

            string ma = TaoMaVaiTro(ten);
            var trungMa = await _context.Roles.FirstOrDefaultAsync(r => r.Code == ma);
            if (trungMa != null)
            {
                return trungMa.Id;
            }

            var vaiTroMoi = new Role
            {
                Code = ma,
                Name = ten,
                Description = "Vai trò do quản trị viên tự thêm từ mục \"Khác\".",
                IsSystem = false,
                IsActive = true,
                CreatedAt = VietnamTime.Now
            };

            _context.Roles.Add(vaiTroMoi);
            await _context.SaveChangesAsync();
            return vaiTroMoi.Id;
        }

        /// <summary>
        /// Dựng mã vai trò từ tên tiếng Việt: bỏ dấu, viết hoa, thay khoảng trắng bằng gạch
        /// dưới. Ví dụ "Trưởng ca bán hàng" thành "TRUONG_CA_BAN_HANG". Mã tối đa 50 ký tự
        /// theo ràng buộc của bảng Roles.
        /// </summary>
        private static string TaoMaVaiTro(string ten)
        {
            string thuong = ten.Replace('đ', 'd').Replace('Đ', 'D');
            string tach = thuong.Normalize(NormalizationForm.FormD);

            var sb = new StringBuilder();
            foreach (char c in tach)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(char.ToUpperInvariant(c));
                }
                else if (sb.Length > 0 && sb[sb.Length - 1] != '_')
                {
                    sb.Append('_');
                }
            }

            string ma = sb.ToString().Trim('_');
            if (ma.Length == 0)
            {
                ma = "VAITRO";
            }

            if (ma.Length > 50)
            {
                ma = ma.Substring(0, 50).Trim('_');
            }

            return ma;
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
