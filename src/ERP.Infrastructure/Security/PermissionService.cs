using ERP.Application.Common.Interfaces;
using ERP_HRM_CMS.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP_HRM_CMS.Helpers
{
    /// <summary>
    /// Xác định quyền hiệu lực của người dùng có tích hợp bộ nhớ đệm trung tâm (IMemoryCache).
    /// Công thức:
    /// 1. IsSuperAdmin = true -> Toàn bộ danh sách quyền hệ thống.
    /// 2. Quyền thực tế = (RolePermissions UNION UserPermissions[IsGranted = 1]) EXCEPT UserPermissions[IsGranted = 0].
    /// Cache tự động bị xóa ngay khi cập nhật User, Role, Permission, RolePermission, UserRole hoặc UserPermission.
    /// </summary>
    public class PermissionService : IPermissionService
    {
        private static readonly TimeSpan PermissionCacheTtl = TimeSpan.FromMinutes(20);

        private readonly AppDbContext _context;
        private readonly IAppCacheService? _cache;

        public PermissionService(AppDbContext context, IAppCacheService? cache = null)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<bool> HasPermissionAsync(int userId, string permissionCode)
        {
            if (userId <= 0 || string.IsNullOrWhiteSpace(permissionCode))
                return false;

            var effectiveSet = await GetEffectivePermissionSetAsync(userId);
            return effectiveSet.Contains(permissionCode.Trim());
        }

        public async Task<List<string>> GetEffectivePermissionsAsync(int userId)
        {
            if (userId <= 0)
                return new List<string>();

            var effectiveSet = await GetEffectivePermissionSetAsync(userId);
            return effectiveSet.ToList();
        }

        private async Task<HashSet<string>> GetEffectivePermissionSetAsync(int userId)
        {
            if (_cache == null)
            {
                return await LoadEffectivePermissionSetFromDbAsync(userId, CancellationToken.None);
            }

            string cacheKey = AppCacheKeys.UserPermissions(userId);
            return await _cache.GetOrCreateAsync(
                cacheKey,
                ct => LoadEffectivePermissionSetFromDbAsync(userId, ct),
                PermissionCacheTtl);
        }

        private async Task<HashSet<string>> LoadEffectivePermissionSetFromDbAsync(int userId, CancellationToken ct)
        {
            var user = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new { u.IsActive, u.IsSuperAdmin })
                .FirstOrDefaultAsync(ct);

            if (user == null || !user.IsActive)
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (user.IsSuperAdmin)
            {
                List<string> allCodes;
                if (_cache != null)
                {
                    allCodes = await _cache.GetOrCreateAsync(
                        AppCacheKeys.AllPermissionCodesKey,
                        token => _context.Permissions.AsNoTracking().Select(p => p.Code).ToListAsync(token),
                        PermissionCacheTtl,
                        ct);
                }
                else
                {
                    allCodes = await _context.Permissions.AsNoTracking().Select(p => p.Code).ToListAsync(ct);
                }

                return new HashSet<string>(allCodes, StringComparer.OrdinalIgnoreCase);
            }

            var rolePerms = await (from ur in _context.UserRoles.AsNoTracking()
                                   join r in _context.Roles.AsNoTracking() on ur.RoleId equals r.Id
                                   join rp in _context.RolePermissions.AsNoTracking() on r.Id equals rp.RoleId
                                   join p in _context.Permissions.AsNoTracking() on rp.PermissionId equals p.Id
                                   where ur.UserId == userId && r.IsActive
                                   select p.Code).Distinct().ToListAsync(ct);

            var userOverrides = await (from up in _context.UserPermissions.AsNoTracking()
                                       join p in _context.Permissions.AsNoTracking() on up.PermissionId equals p.Id
                                       where up.UserId == userId
                                       select new { p.Code, up.IsGranted }).ToListAsync(ct);

            var effective = new HashSet<string>(rolePerms, StringComparer.OrdinalIgnoreCase);

            foreach (var item in userOverrides)
            {
                if (item.IsGranted)
                    effective.Add(item.Code);
            }

            foreach (var item in userOverrides)
            {
                if (!item.IsGranted)
                    effective.Remove(item.Code);
            }

            return effective;
        }
    }
}

