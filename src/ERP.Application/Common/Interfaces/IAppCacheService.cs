namespace ERP.Application.Common.Interfaces;

/// <summary>
/// Dịch vụ bộ nhớ đệm trung tâm (IMemoryCache) hỗ trợ tự động hủy cache theo khóa và tiền tố
/// cho Danh sách Quyền (PermissionService), SiteSettings và DynamicMenu / Banner.
/// </summary>
public interface IAppCacheService
{
    T GetOrCreate<T>(string key, Func<T> factory, TimeSpan? absoluteExpiration = null);

    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? absoluteExpiration = null,
        CancellationToken cancellationToken = default);

    void Remove(string key);
    void RemoveByPrefix(string prefix);

    void InvalidatePermissions(int? userId = null);
    void InvalidateSiteSettings();
    void InvalidateMenus();
}

public static class AppCacheKeys
{
    public const string PermissionsPrefix = "erp:perms:";
    public const string AllPermissionCodesKey = "erp:perms:all-codes";
    public const string SiteSettingsKey = "erp:cms:site-settings";
    public const string DynamicMenuPrefix = "erp:cms:menu:";
    public const string DynamicBannerPrefix = "erp:cms:banner:";

    public static string UserPermissions(int userId) => $"{PermissionsPrefix}user:{userId}";
    public static string DynamicMenuTree(string location) => $"{DynamicMenuPrefix}tree:{location.ToUpperInvariant()}";
    public static string DynamicBanner(string position) => $"{DynamicBannerPrefix}pos:{position.ToUpperInvariant()}";
}

