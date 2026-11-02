using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Filters;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.ViewModels;

namespace ERP_HRM_CMS.Controllers
{
    [Authorize]
    [KiemTraQuyen("auth.matrix.view")]
    public class PhanQuyenController : Controller
    {
        private readonly AppDbContext _context;

        public PhanQuyenController(AppDbContext context)
        {
            _context = context;
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
            if (dto.RoleId <= 0) return BadRequest(new { success = false, message = "Vai trò không hợp lệ" });

            var role = await _context.Roles.FindAsync(dto.RoleId);
            if (role == null) return NotFound(new { success = false, message = "Không tìm thấy vai trò" });

            // Xóa toàn bộ mapping cũ của Role
            var oldMappings = _context.RolePermissions.Where(rp => rp.RoleId == dto.RoleId);
            _context.RolePermissions.RemoveRange(oldMappings);

            // Thêm các quyền được tích chọn
            if (dto.PermissionIds != null && dto.PermissionIds.Any())
            {
                var newMappings = dto.PermissionIds.Distinct().Select(pId => new RolePermission
                {
                    RoleId = dto.RoleId,
                    PermissionId = pId
                });
                await _context.RolePermissions.AddRangeAsync(newMappings);
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
            if (dto.UserId <= 0) return BadRequest(new { success = false, message = "Người dùng không hợp lệ" });

            // Xóa các ghi đè cũ của cá nhân
            var oldAcls = _context.UserPermissions.Where(up => up.UserId == dto.UserId);
            _context.UserPermissions.RemoveRange(oldAcls);

            if (dto.Overrides != null && dto.Overrides.Any())
            {
                foreach (var item in dto.Overrides)
                {
                    if (item.State.HasValue)
                    {
                        _context.UserPermissions.Add(new UserPermission
                        {
                            UserId = dto.UserId,
                            PermissionId = item.PermissionId,
                            IsGranted = item.State.Value,
                            AssignedAt = DateTime.Now
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Cập nhật ngoại lệ phân quyền cá nhân thành công!" });
        }
    }
}