using System.Globalization;
using System.Net;
using System.Text;

namespace ERP_HRM_CMS.Services.Printing
{
    /// <summary>
    /// [OOP - KẾ THỪA] Lớp cơ sở trừu tượng cho mọi phiếu in.
    /// PhieuLuongPrintable, BangLuongPrintable, HoaDonPrintable đều kế thừa lớp này.
    ///
    /// [OOP - ĐÓNG GÓI] _tieuDe và _thoiDiemIn là private, chỉ đọc qua thuộc tính.
    /// Mã hóa HTML và định dạng tiền nằm ở đây, lớp con không thể quên mã hóa dữ liệu người dùng.
    ///
    /// [OOP - TEMPLATE METHOD] RenderBody() cố định thứ tự: đầu trang -> nội dung -> chân trang.
    /// Lớp con bắt buộc thay NoiDungChinh() (abstract), và có thể override đầu/chân trang (virtual).
    /// </summary>
    public abstract class PrintableDocument : IPrintable
    {
        private static readonly CultureInfo ViVN = new CultureInfo("vi-VN");

        private readonly string _tieuDe;
        private readonly DateTime _thoiDiemIn;

        protected PrintableDocument(string tieuDe)
        {
            if (string.IsNullOrWhiteSpace(tieuDe))
            {
                throw new ArgumentException("Tiêu đề phiếu in không được để trống.", nameof(tieuDe));
            }

            _tieuDe = tieuDe.Trim();
            _thoiDiemIn = DateTime.Now;
        }

        // [OOP - ĐÓNG GÓI] Chỉ đọc từ bên ngoài
        public string TieuDe => _tieuDe;

        protected DateTime ThoiDiemIn => _thoiDiemIn;

        /// <summary>[TEMPLATE METHOD] Thứ tự cố định, lớp con không thể đảo thứ tự.</summary>
        public string RenderBody()
        {
            return RenderHeader() + NoiDungChinh() + RenderFooter();
        }

        public string RenderHtml()
        {
            return TrangIn.Boc(_tieuDe, "<div class=\"phieu\">" + RenderBody() + "</div>");
        }

        /// <summary>[OOP - ĐA HÌNH, có thể override] Đầu trang mặc định: tiêu đề và thời điểm in.</summary>
        protected virtual string RenderHeader()
        {
            return "<h1>" + Enc(_tieuDe) + "</h1>" +
                   "<p class=\"ngay\">In lúc " + _thoiDiemIn.ToString("dd/MM/yyyy HH:mm", ViVN) + "</p>";
        }

        /// <summary>[OOP - ĐA HÌNH, có thể override] Chân trang mặc định: dòng ghi chú chung.</summary>
        protected virtual string RenderFooter()
        {
            return "<p class=\"ghichu\">Phiếu được tạo tự động bởi hệ thống ERP Tiên Phong.</p>";
        }

        /// <summary>[OOP - ĐA HÌNH, bắt buộc override] Mỗi loại phiếu tự quyết định nội dung chính.</summary>
        protected abstract string NoiDungChinh();

        /// <summary>Mã hóa HTML. Mọi dữ liệu người dùng nhập đều phải đi qua hàm này.</summary>
        protected static string Enc(string giaTri)
        {
            return WebUtility.HtmlEncode(giaTri ?? string.Empty);
        }

        /// <summary>Định dạng tiền Việt Nam, ví dụ 9.000.000 đ.</summary>
        protected static string Tien(decimal soTien)
        {
            return soTien.ToString("N0", ViVN) + " đ";
        }

        /// <summary>Một dòng hai cột. Nhãn được mã hóa, nội dung đã được mã hóa từ trước.</summary>
        protected static string Dong(string nhan, string noiDungDaMaHoa, bool dam = false)
        {
            string lop = dam ? " class=\"dam\"" : string.Empty;
            return "<tr" + lop + "><td>" + Enc(nhan) + "</td><td class=\"so\">" + noiDungDaMaHoa + "</td></tr>";
        }
    }
}
