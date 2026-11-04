using System.ComponentModel.DataAnnotations;

namespace ERP_HRM_CMS.Helpers
{
    /// <summary>
    /// Kiểm soát đường dẫn do người dùng nhập (ví dụ CustomUrl của menu).
    /// Chỉ chấp nhận: bắt đầu bằng "/" (đường dẫn nội bộ), "https://", "http://", "mailto:".
    /// Từ chối javascript:, data:, vbscript: và đường dẫn "//host" (trỏ sang máy chủ khác).
    /// </summary>
    public static class UrlAnToan
    {
        private static readonly string[] TienToNgoaiChoPhep = { "https://", "http://", "mailto:" };

        public static bool LaHopLe(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return true;
            }

            string u = url.Trim();

            if (u.Any(c => char.IsControl(c)))
            {
                return false;
            }

            if (u[0] == '/')
            {
                // "//host" và "/\host" được trình duyệt coi là URL sang máy chủ khác
                return u.Length == 1 || (u[1] != '/' && u[1] != '\\');
            }

            foreach (var tien in TienToNgoaiChoPhep)
            {
                if (u.StartsWith(tien, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public class UrlAnToanAttribute : ValidationAttribute
    {
        public UrlAnToanAttribute()
            : base("Đường dẫn không hợp lệ. Chỉ chấp nhận đường dẫn bắt đầu bằng '/', 'https://', 'http://' hoặc 'mailto:'.")
        {
        }

        public override bool IsValid(object? value)
        {
            return UrlAnToan.LaHopLe(value as string);
        }
    }
}
