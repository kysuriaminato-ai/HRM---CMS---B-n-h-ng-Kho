using Microsoft.EntityFrameworkCore;
using ERP_HRM_CMS.Data;

namespace ERP_HRM_CMS.Helpers
{
    public interface IPermissionService
    {
        Task<bool> HasPermissionAsync(int userId, string permissionCode);
        Task<List<string>> GetEffectivePermissionsAsync(int userId);
    }

    public class PermissionService : IPermissionService
    {
        private readonly AppDbContext _context;

        public PermissionService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Công thức xác định quyền thực tế:
        /// 1. Nếu IsSuperAdmin = true -> Trả về true (Toàn quyền).
        /// 2. Quyền thực tế = (Quyền kế thừa từ các Roles) HỢP (UserPermissions có IsGranted = 1) TRỪ (UserPermissions có IsGranted = 0).
        /// </summary>
        public async Task<bool> HasPermissionAsync(int userId, string permissionCode)
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null || !user.IsActive) return false;

            // Super Admin luôn có toàn quyền
            if (user.IsSuperAdmin) return true;

            // 1. Kiểm tra quyền cá nhân bị CẤM (IsGranted = false) -> Ưu tiên chặn cao nhất
            bool isExplicitlyDenied = await (from up in _context.UserPermissions
                                             join p in _context.Permissions on up.PermissionId equals p.Id
                                             where up.UserId == userId && p.Code == permissionCode && !up.IsGranted
                                             select up).AnyAsync();
            if (isExplicitlyDenied) return false;

            // 2. Kiểm tra quyền cá nhân được CẤP VƯỢT CẤP (IsGranted = true)
            bool isExplicitlyGranted = await (from up in _context.UserPermissions
                                              join p in _context.Permissions on up.PermissionId equals p.Id
                                              where up.UserId == userId && p.Code == permissionCode && up.IsGranted
                                              select up).AnyAsync();
            if (isExplicitlyGranted) return true;

            // 3. Kiểm tra kế thừa từ các Role của User
            bool hasRolePermission = await (from ur in _context.UserRoles
                                            join rp in _context.RolePermissions on ur.RoleId equals rp.RoleId
                                            join p in _context.Permissions on rp.PermissionId equals p.Id
                                            where ur.UserId == userId && p.Code == permissionCode
                                            select rp).AnyAsync();

            return hasRolePermission;
        }

        public async Task<List<string>> GetEffectivePermissionsAsync(int userId)
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null || !user.IsActive) return new List<string>();

            if (user.IsSuperAdmin)
            {
                return await _context.Permissions.Select(p => p.Code).ToListAsync();
            }

            // Tập quyền từ Roles
            var rolePerms = await (from ur in _context.UserRoles
                                   join rp in _context.RolePermissions on ur.RoleId equals rp.RoleId
                                   join p in _context.Permissions on rp.PermissionId equals p.Id
                                   where ur.UserId == userId
                                   select p.Code).Distinct().ToListAsync();

            // Quyền cá nhân được cấp thêm (IsGranted = true)
            var grantedPerms = await (from up in _context.UserPermissions
                                      join p in _context.Permissions on up.PermissionId equals p.Id
                                      where up.UserId == userId && up.IsGranted
                                      select p.Code).ToListAsync();

            // Quyền cá nhân bị thu hồi (IsGranted = false)
            var deniedPerms = await (from up in _context.UserPermissions
                                     join p in _context.Permissions on up.PermissionId equals p.Id
                                     where up.UserId == userId && !up.IsGranted
                                     select p.Code).ToListAsync();

            // Áp dụng công thức: (RolePerms UNION GrantedPerms) EXCEPT DeniedPerms
            var effective = rolePerms.Union(grantedPerms).Except(deniedPerms).Distinct().ToList();
            return effective;
        }
    }
}