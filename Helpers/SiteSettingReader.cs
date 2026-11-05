using ERP.Application.Common.Interfaces;
using ERP_HRM_CMS.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP_HRM_CMS.Helpers
{
    /// <summary>
    /// Đọc một giá trị cấu hình site (bảng SiteSettings) cho view qua bộ nhớ đệm tập trung IMemoryCache.
    /// Tự động xóa cache khi cập nhật SiteSettings.
    /// </summary>
    public static class SiteSettingReader
    {
        private static readonly TimeSpan SettingsCacheTtl = TimeSpan.FromMinutes(30);

        public static string Get(AppDbContext db, string key, string fallback)
        {
            if (db.AppCache != null)
            {
                var map = db.AppCache.GetOrCreate(
                    AppCacheKeys.SiteSettingsKey,
                    () => db.SiteSettings
                        .AsNoTracking()
                        .ToDictionary(s => s.SettingKey, s => s.SettingValue, StringComparer.OrdinalIgnoreCase),
                    SettingsCacheTtl);

                if (map.TryGetValue(key, out var cachedValue) && !string.IsNullOrWhiteSpace(cachedValue))
                {
                    return cachedValue;
                }

                return fallback;
            }

            var value = db.SiteSettings
                .AsNoTracking()
                .Where(s => s.SettingKey == key)
                .Select(s => s.SettingValue)
                .FirstOrDefault();

            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }
    }
}
