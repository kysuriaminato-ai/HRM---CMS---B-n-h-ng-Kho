using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace ERP_HRM_CMS.Helpers
{
    /// <summary>
    /// Làm sạch HTML của nội dung CMS trước khi hiển thị công khai.
    /// Chỉ giữ thẻ định dạng cơ bản và thuộc tính an toàn; loại bỏ script, thuộc tính sự kiện on*, style,
    /// và mọi URL không phải http/https/mailto hoặc đường dẫn nội bộ.
    /// Thẻ không nằm trong danh sách bị bỏ, phần chữ bên trong vẫn được giữ lại.
    /// </summary>
    public static class HtmlSanitizerHelper
    {
        private static readonly HashSet<string> AllowedTags = new(StringComparer.OrdinalIgnoreCase)
        {
            "p", "br", "hr", "strong", "b", "em", "i", "u", "s", "small", "span", "div",
            "blockquote", "pre", "code", "ul", "ol", "li",
            "h1", "h2", "h3", "h4", "h5", "h6",
            "a", "img",
            "table", "caption", "thead", "tbody", "tfoot", "tr", "th", "td"
        };

        // Các thẻ nguy hiểm: xóa cả nội dung bên trong (hoặc chỉ thẻ mở nếu không có thẻ đóng).
        private static readonly Regex DroppedBlockRegex = new(
            @"<(script|style|iframe|object|embed|noscript|template|svg|math|textarea|title|xml)\b[^>]*>.*?</\1\s*>|<(script|style|iframe|object|embed|noscript|template|svg|math|link|meta|base|form|input|button|select)\b[^>]*>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly Regex CommentRegex = new(
            @"<!--.*?-->", RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly Regex TagRegex = new(
            @"<(/?)([a-zA-Z][a-zA-Z0-9]*)((?:\s+[^\s""'>/=]+(?:\s*=\s*(?:""[^""]*""|'[^']*'|[^\s""'=<>`]+))?)*)\s*(/?)>",
            RegexOptions.Compiled);

        private static readonly Regex AttributeRegex = new(
            @"([^\s""'>/=]+)(?:\s*=\s*(?:""([^""]*)""|'([^']*)'|([^\s""'=<>`]+)))?",
            RegexOptions.Compiled);

        public static string Sanitize(string? html)
        {
            if (string.IsNullOrEmpty(html)) return string.Empty;

            string input = CommentRegex.Replace(html, string.Empty);
            input = DroppedBlockRegex.Replace(input, string.Empty);

            // Chỉ các thẻ được tạo lại từ danh sách cho phép mới xuất hiện trong kết quả.
            var sb = new StringBuilder(input.Length);
            int last = 0;
            foreach (Match tag in TagRegex.Matches(input))
            {
                AppendText(sb, input.Substring(last, tag.Index - last));
                AppendTag(sb, tag);
                last = tag.Index + tag.Length;
            }
            AppendText(sb, input.Substring(last));

            return sb.ToString();
        }

        // Chữ nằm ngoài thẻ: mã hóa < và > để không thể mở thẻ mới.
        private static void AppendText(StringBuilder sb, string text)
        {
            sb.Append(text.Replace("<", "&lt;").Replace(">", "&gt;"));
        }

        private static void AppendTag(StringBuilder sb, Match tag)
        {
            bool closing = tag.Groups[1].Value == "/";
            string name = tag.Groups[2].Value.ToLowerInvariant();
            if (!AllowedTags.Contains(name)) return;

            if (closing)
            {
                sb.Append("</").Append(name).Append('>');
                return;
            }

            sb.Append('<').Append(name);
            foreach (Match attr in AttributeRegex.Matches(tag.Groups[3].Value))
            {
                string attrName = attr.Groups[1].Value.ToLowerInvariant();
                string raw = attr.Groups[2].Success ? attr.Groups[2].Value
                    : attr.Groups[3].Success ? attr.Groups[3].Value
                    : attr.Groups[4].Value;
                string value = WebUtility.HtmlDecode(raw);

                if (!IsAllowedAttribute(name, attrName, value)) continue;
                sb.Append(' ').Append(attrName).Append("=\"").Append(WebUtility.HtmlEncode(value)).Append('"');
            }

            if (name == "a")
            {
                sb.Append(" rel=\"noopener noreferrer nofollow\"");
            }
            sb.Append('>');
        }

        private static bool IsAllowedAttribute(string tag, string attr, string value)
        {
            switch (attr)
            {
                case "href":
                    return tag == "a" && IsSafeUrl(value);
                case "src":
                    return tag == "img" && IsSafeUrl(value);
                case "alt":
                case "title":
                    return true;
                case "colspan":
                case "rowspan":
                    return value.Length > 0 && value.Length <= 3 && value.All(char.IsDigit);
                default:
                    return false;
            }
        }

        private static bool IsSafeUrl(string url)
        {
            // Bỏ khoảng trắng và ký tự điều khiển để không lách qua kiểm tra (ví dụ "java\tscript:")
            string cleaned = new string(url.Where(c => !char.IsWhiteSpace(c) && !char.IsControl(c)).ToArray());
            if (cleaned.Length == 0) return false;
            if (cleaned.StartsWith("#")) return true;
            if (cleaned.StartsWith("/") && !cleaned.StartsWith("//")) return true;

            return Uri.TryCreate(cleaned, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp
                    || uri.Scheme == Uri.UriSchemeHttps
                    || uri.Scheme == Uri.UriSchemeMailto);
        }
    }
}
