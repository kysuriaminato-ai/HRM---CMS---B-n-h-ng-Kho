namespace ERP_HRM_CMS.Helpers;

/// <summary>
/// Hợp đồng kiểm tra quyền hiệu lực của người dùng (kết hợp RolePermissions và UserPermissions).
/// Giữ namespace tương thích ngược để không phá vỡ các Filter, Controller và Razor Views hiện hữu.
/// </summary>
public interface IPermissionService
{
    Task<bool> HasPermissionAsync(int userId, string permissionCode);
    Task<List<string>> GetEffectivePermissionsAsync(int userId);

    async Task<bool> HasAnyPermissionAsync(int userId, params string[] permissionCodes)
    {
        if (permissionCodes == null || permissionCodes.Length == 0) return false;
        var effective = await GetEffectivePermissionsAsync(userId);
        if (effective.Count == 0) return false;
        var set = effective.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return permissionCodes.Any(code => !string.IsNullOrWhiteSpace(code) && set.Contains(code));
    }
}

