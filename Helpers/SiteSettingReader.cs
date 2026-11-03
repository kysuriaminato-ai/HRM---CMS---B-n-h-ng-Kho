using ERP_HRM_CMS.Data;

namespace ERP_HRM_CMS.Helpers
{
    /// <summary>
    /// Đọc một giá trị cấu hình site (bảng SiteSettings) cho view. Thương hiệu hiển thị lấy từ đây,
    /// không gắn cứng trong mã nguồn. Trả về giá trị dự phòng nếu khóa chưa có hoặc để trống.
    /// </summary>
    public static class SiteSettingReader
    {
        public static string Get(AppDbContext db, string key, string fallback)
        {
            var value = db.SiteSettings
                .AsNoTracking()
                .Where(s => s.SettingKey == key)
                .Select(s => s.SettingValue)
                .FirstOrDefault();

            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }
    }
}
