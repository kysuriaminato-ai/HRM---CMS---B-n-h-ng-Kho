using System.Security.Claims;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Filters;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.ViewModels;
using ERP_HRM_CMS.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP_HRM_CMS.Controllers
{
    [Authorize]
    [KiemTraQuyen("auth.matrix.view")]
    public class PhanQuyenController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IPermissionService _permissionService;
        private readonly ISuperAdminService _superAdmin;

        public PhanQuyenController(AppDbContext context, IPermissionService permissionService, ISuperAdminService superAdmin)
        {
            _context = context;
            _permissionService = permissionService;
            _superAdmin = superAdmin;
        }

        [HttpGet]
        public IActionResult Index(int? roleId)
        {
            return RedirectToAction(nameof(MaTranPhanQuyen), new { roleId });
        }

        // 1. GIAO DIỆN MA TRẬN PHÂN QUYỀN CHO VAI TRÒ (RBAC MATRIX)
        [HttpGet]
        public async Task<IActionResult> MaTranPhanQuyen(int? roleId)
        {
            var roles = await _context.Roles
                .Where(r => r.IsActive)
                .Select(r => new RoleOptionDto { Id = r.Id, Code = r.Code, Name = r.Name })
                .ToListAsync();

            int targetRoleId = roleId ?? roles.FirstOrDefault()?.Id ?? 0;

            var modules = await _context.Modules
                .Where(m => m.IsActive)
                .OrderBy(m => m.OrderIndex)
                .Select(m => new ModulePermissionGroupDto
                {
                    ModuleId = m.Id,
                    ModuleName = m.Name,
                    ModuleIcon = m.Icon,
                    Permissions = m.Permissions
                        .OrderBy(p => p.OrderIndex)
                        .Select(p => new PermissionItemDto
                        {
                            Id = p.Id,
                            Code = p.Code,
                            Name = p.Name,
                            Description = p.Description
                        }).ToList()
                }).ToListAsync();

            var activePermissionIds = await _context.RolePermissions
                .Where(rp => rp.RoleId == targetRoleId)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            var vm = new MaTranPhanQuyenViewModel
            {
                SelectedRoleId = targetRoleId,
                Roles = roles,
                Modules = modules,
                ActivePermissionIds = activePermissionIds
            };

            return View(vm);
        }

        // Cập nhật ma trận quyền Role qua AJAX
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("auth.matrix.update")]
        public async Task<IActionResult> LuuMaTranQuyenRole([FromBody] CapNhatQuyenRoleDto dto)
        {
            if (dto == null || dto.RoleId <= 0) return LoiJson(400, "Vai trò không hợp lệ");

            var role = await _context.Roles.FindAsync(dto.RoleId);
            if (role == null) return LoiJson(404, "Không tìm thấy vai trò");

            int nguoiThaoTac = CurrentUserId();
            if (nguoiThaoTac <= 0) return LoiJson(403, PermissionDelegationGuard.ThongBaoKhongDuQuyen);

            bool laSuperAdmin = await _superAdmin.LaSuperAdminAsync(nguoiThaoTac);

            // Chỉ SuperAdmin được sửa vai trò SUPERADMIN
            if (string.Equals(role.Code, SuperAdminService.MaVaiTroSuperAdmin, StringComparison.OrdinalIgnoreCase)
                && !laSuperAdmin)
            {
                return LoiJson(403, "Chỉ SuperAdmin mới được sửa quyền của vai trò SUPERADMIN.");
            }

            var yeuCau = (dto.PermissionIds ?? new List<int>()).Distinct().ToList();

            // Mọi mã quyền gửi lên phải tồn tại
            int soQuyenHopLe = await _context.Permissions.CountAsync(p => yeuCau.Contains(p.Id));
            if (soQuyenHopLe != yeuCau.Count) return LoiJson(400, "Có mã quyền không tồn tại.");

            var hienTai = await _context.RolePermissions
                .Where(rp => rp.RoleId == role.Id)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            // Chỉ kiểm tra các quyền thực sự được thêm hoặc bỏ
            var idThayDoi = hienTai.Except(yeuCau).Union(yeuCau.Except(hienTai)).ToList();
            var thayDoi = await _context.Permissions
                .Where(p => idThayDoi.Contains(p.Id))
                .Select(p => new { p.Id, p.Code })
                .ToListAsync();

            var quyenHienCo = await _permissionService.GetEffectivePermissionsAsync(nguoiThaoTac);
            string? loi = PermissionDelegationGuard.KiemTra(
                thayDoi.Select(p => (p.Id, p.Code)),
                new HashSet<string>(quyenHienCo, StringComparer.OrdinalIgnoreCase),
                laSuperAdmin);
            if (loi != null) return LoiJson(403, loi);

            var xoa = await _context.RolePermissions
                .Where(rp => rp.RoleId == role.Id && !yeuCau.Contains(rp.PermissionId))
                .ToListAsync();
            _context.RolePermissions.RemoveRange(xoa);

            foreach (int permissionId in yeuCau.Except(hienTai))
            {
                _context.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permissionId });
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = $"Cập nhật quyền cho nhóm '{role.Name}' thành công!" });
        }

        // 2. GIAO DIỆN PHÂN QUYỀN ĐẶC THÙ CÁ NHÂN (ACL OVERRIDE MATRIX)
        [HttpGet]
        public async Task<IActionResult> PhanQuyenCaNhan(int? userId)
        {
            var users = await _context.Users
                .Include(u => u.Employee)
                .Where(u => u.IsActive)
                .Select(u => new UserOptionDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    FullName = u.Employee != null ? u.Employee.FullName : u.Username
                }).ToListAsync();

            int targetUserId = userId ?? users.FirstOrDefault()?.Id ?? 0;
            var targetUser = await _context.Users.Include(u => u.Employee).FirstOrDefaultAsync(u => u.Id == targetUserId);

            var modules = await _context.Modules
                .Where(m => m.IsActive)
                .OrderBy(m => m.OrderIndex)
                .Select(m => new ModulePermissionGroupDto
                {
                    ModuleId = m.Id,
                    ModuleName = m.Name,
                    ModuleIcon = m.Icon,
                    Permissions = m.Permissions.OrderBy(p => p.OrderIndex).Select(p => new PermissionItemDto
                    {
                        Id = p.Id,
                        Code = p.Code,
                        Name = p.Name,
                        Description = p.Description
                    }).ToList()
                }).ToListAsync();

            // Quyền kế thừa từ Role của User
            var rolePermIds = await (from ur in _context.UserRoles
                                     join rp in _context.RolePermissions on ur.RoleId equals rp.RoleId
                                     where ur.UserId == targetUserId
                                     select rp.PermissionId).Distinct().ToListAsync();

            // Quyền cá nhân hóa (ACL Override)
            var userOverrides = await _context.UserPermissions
                .Where(up => up.UserId == targetUserId)
                .ToDictionaryAsync(up => up.PermissionId, up => up.IsGranted);

            var vm = new PhanQuyenCaNhanViewModel
            {
                SelectedUserId = targetUserId,
                Username = targetUser?.Username ?? "",
                FullName = targetUser?.Employee?.FullName ?? targetUser?.Username ?? "",
                Users = users,
                Modules = modules,
                RoleInheritedPermissionIds = rolePermIds,
                UserAclOverrides = userOverrides
            };

            return View(vm);
        }

        // Cập nhật ma trận ACL cá nhân qua AJAX
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("auth.matrix.update")]
        public async Task<IActionResult> LuuPhanQuyenCaNhan([FromBody] CapNhatQuyenCaNhanDto dto)
        {
            if (dto == null || dto.UserId <= 0) return LoiJson(400, "Người dùng không hợp lệ");

            var gui = dto.Overrides ?? new List<UserPermissionItemDto>();
            var idGui = gui.Select(o => o.PermissionId).ToList();
            if (idGui.Distinct().Count() != idGui.Count) return LoiJson(400, "Dữ liệu quyền bị trùng.");

            bool tonTai = await _context.Users.AsNoTracking().AnyAsync(u => u.Id == dto.UserId);
            if (!tonTai) return LoiJson(404, "Không tìm thấy người dùng");

            int nguoiThaoTac = CurrentUserId();
            if (nguoiThaoTac <= 0) return LoiJson(403, PermissionDelegationGuard.ThongBaoKhongDuQuyen);

            bool laSuperAdmin = await _superAdmin.LaSuperAdminAsync(nguoiThaoTac);

            // Tài khoản quản trị tổng chỉ SuperAdmin được thay đổi quyền
            if (!laSuperAdmin && await _superAdmin.LaTaiKhoanQuanTriTongAsync(dto.UserId))
            {
                return LoiJson(403, "Chỉ SuperAdmin mới được thay đổi quyền của tài khoản quản trị tổng.");
            }

            int soQuyenHopLe = await _context.Permissions.CountAsync(p => idGui.Contains(p.Id));
            if (soQuyenHopLe != idGui.Count) return LoiJson(400, "Có mã quyền không tồn tại.");

            var cu = await _context.UserPermissions.Where(up => up.UserId == dto.UserId).ToListAsync();
            var cuMap = cu.ToDictionary(up => up.PermissionId, up => up.IsGranted);
            var moiMap = gui.Where(o => o.State.HasValue).ToDictionary(o => o.PermissionId, o => o.State!.Value);

            // Chỉ xét các quyền có trạng thái thay đổi (được cấp riêng, bị chặn riêng, hoặc bỏ ghi đè)
            var idThayDoi = new List<int>();
            foreach (int id in cuMap.Keys.Union(moiMap.Keys))
            {
                bool coCu = cuMap.TryGetValue(id, out bool trangThaiCu);
                bool coMoi = moiMap.TryGetValue(id, out bool trangThaiMoi);
                if (coCu != coMoi || (coCu && trangThaiCu != trangThaiMoi))
                {
                    idThayDoi.Add(id);
                }
            }

            var thayDoi = await _context.Permissions
                .Where(p => idThayDoi.Contains(p.Id))
                .Select(p => new { p.Id, p.Code })
                .ToListAsync();

            var quyenHienCo = await _permissionService.GetEffectivePermissionsAsync(nguoiThaoTac);
            string? loi = PermissionDelegationGuard.KiemTra(
                thayDoi.Select(p => (p.Id, p.Code)),
                new HashSet<string>(quyenHienCo, StringComparer.OrdinalIgnoreCase),
                laSuperAdmin);
            if (loi != null) return LoiJson(403, loi);

            var thoiDiem = VietnamTime.Now;
            foreach (int id in idThayDoi)
            {
                var dong = cu.FirstOrDefault(up => up.PermissionId == id);
                bool coMoi = moiMap.TryGetValue(id, out bool trangThaiMoi);

                if (dong != null && coMoi)
                {
                    dong.IsGranted = trangThaiMoi;
                    dong.AssignedAt = thoiDiem;
                }
                else if (dong != null)
                {
                    _context.UserPermissions.Remove(dong);
                }
                else if (coMoi)
                {
                    _context.UserPermissions.Add(new UserPermission
                    {
                        UserId = dto.UserId,
                        PermissionId = id,
                        IsGranted = trangThaiMoi,
                        AssignedAt = thoiDiem
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Cập nhật ngoại lệ phân quyền cá nhân thành công!" });
        }

        private int CurrentUserId()
        {
            return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
        }

        private IActionResult LoiJson(int statusCode, string message)
        {
            return StatusCode(statusCode, new { success = false, message });
        }
    }
}
