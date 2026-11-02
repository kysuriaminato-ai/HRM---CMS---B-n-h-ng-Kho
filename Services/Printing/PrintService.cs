using System.Globalization;
using System.Text;

namespace ERP_HRM_CMS.Services.Printing
{
    /// <summary>
    /// [OOP - ĐA HÌNH] PrintService chỉ làm việc với IPrintable.
    /// Thêm loại phiếu mới (ví dụ phiếu nhập kho) chỉ cần tạo một lớp kế thừa PrintableDocument,
    /// không phải sửa lớp này.
    /// </summary>
    public class PrintService : IPrintService
    {
        public string Render(IPrintable document)
        {
            ArgumentNullException.ThrowIfNull(document);
            return document.RenderHtml();
        }

        public string RenderBatch(IEnumerable<IPrintable> documents)
        {
            ArgumentNullException.ThrowIfNull(documents);

            var sb = new StringBuilder();
            int soPhieu = 0;
            foreach (var doc in documents)
            {
                // [OOP - ĐA HÌNH] Mỗi phần tử tự biết cách trình bày phần thân của mình.
                // Mỗi phiếu nằm trên một trang riêng khi in (class "trang").
                sb.Append("<div class=\"trang\"><div class=\"phieu\">")
                  .Append(doc.RenderBody())
                  .Append("</div></div>");
                soPhieu++;
            }

            string tieuDe = "In hàng loạt (" + soPhieu.ToString(CultureInfo.InvariantCulture) + " phiếu)";
            return TrangIn.Boc(tieuDe, sb.ToString());
        }
    }
}
