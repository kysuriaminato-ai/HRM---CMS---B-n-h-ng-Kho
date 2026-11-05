using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ERP_HRM_CMS.Helpers
{
    public static class SlugHelper
    {
        public static string GenerateSlug(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            string normalized = text.Trim().ToLowerInvariant();

            // Thay thế chữ 'đ', 'Đ'
            normalized = normalized.Replace("đ", "d").Replace("Đ", "d");

            // Tách dấu tiếng Việt
            string normalizedString = normalized.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (char c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            string result = stringBuilder.ToString().Normalize(NormalizationForm.FormC);

            // Xóa ký tự đặc biệt, chỉ giữ lại a-z, 0-9, dấu cách và gạch nối
            result = Regex.Replace(result, @"[^a-z0-9\s-]", "");

            // Thay thế khoảng trắng hoặc nhiều gạch nối liên tiếp thành 1 gạch nối
            result = Regex.Replace(result, @"[\s-]+", "-").Trim('-');

            return result;
        }
    }
}

