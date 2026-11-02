using System.Net;

namespace ERP_HRM_CMS.Services.Printing
{
    /// <summary>
    /// [OOP - ĐÓNG GÓI] Khung trang HTML và CSS in dùng chung, gói ở một nơi.
    /// Muốn đổi giao diện in chỉ cần sửa file này, mọi loại phiếu tự nhận thay đổi.
    /// </summary>
    public static class TrangIn
    {
        private const string Css =
            "body{font-family:'Segoe UI',Arial,sans-serif;color:#111;margin:0;padding:24px;background:#fff}" +
            ".phieu{max-width:780px;margin:0 auto}" +
            "h1{font-size:22px;text-align:center;margin:0 0 4px}" +
            ".ngay{text-align:center;color:#555;font-size:12px;margin:0 0 16px}" +
            "table{width:100%;border-collapse:collapse;margin-top:8px;font-size:13px}" +
            "th,td{border:1px solid #bbb;padding:6px 8px;text-align:left}" +
            "th{background:#f1f3f5}" +
            "td.so,th.so{text-align:right}" +
            ".dam td{font-weight:bold;background:#fafafa}" +
            ".ky{display:flex;justify-content:space-around;margin-top:40px;text-align:center;font-size:13px}" +
            ".ghichu{margin-top:24px;font-size:11px;color:#777;text-align:center}" +
            ".trang{page-break-after:always}" +
            "@media print{body{padding:0}}";

        /// <summary>Bọc nội dung HTML vào khung trang đầy đủ, tiêu đề được mã hóa.</summary>
        public static string Boc(string tieuDe, string noiDungHtml)
        {
            return "<!DOCTYPE html><html lang=\"vi\"><head><meta charset=\"utf-8\"><title>" +
                   WebUtility.HtmlEncode(tieuDe ?? string.Empty) +
                   "</title><style>" + Css + "</style></head><body>" +
                   noiDungHtml +
                   "</body></html>";
        }
    }
}
