using System.Net;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.Enums;
using ERP_HRM_CMS.Services.Printing;
using Xunit;

namespace ERP_HRM_CMS.Tests
{
    /// <summary>Kiểm thử lớp in ấn: kế thừa, đa hình, đóng gói và mã hóa dữ liệu. Không cần database.</summary>
    public class PrintingTests
    {
        private static PayrollPeriod KyMau() => new PayrollPeriod
        {
            Id = 1,
            Name = "Bảng lương Tháng 10/2026",
            Month = 10,
            Year = 2026,
            StandardWorkDays = 22,
            Status = PayrollPeriodStatus.Draft
        };

        private static Employee NhanVienMau(string hoTen = "Nguyễn Văn A", string ma = "NV001") => new Employee
        {
            Id = 1,
            EmployeeCode = ma,
            FullName = hoTen,
            Department = new Department { Name = "Kế toán" },
            Position = new Position { Name = "Kế toán viên" }
        };

        private static SalarySlip PhieuMau(Employee nv, decimal thucLinh) => new SalarySlip
        {
            Id = 1,
            EmployeeId = nv.Id,
            BasicSalary = 10_000_000m,
            GrossSalary = 10_000_000m,
            NetSalary = thucLinh,
            TotalInsurance = 1_050_000m,
            Employee = nv
        };

        private static Order DonMau() => new Order
        {
            Id = 1,
            OrderCode = "DH001",
            CustomerName = "Khách lẻ",
            CustomerPhone = "0900000000",
            DiscountAmount = 10_000m,
            FinalAmount = 90_000m,
            TotalAmount = 100_000m,
            Branch = new Branch { Name = "Chi nhánh Cầu Giấy" },
            OrderDetails = new List<OrderDetail>
            {
                new OrderDetail { Quantity = 2, UnitPrice = 50_000m, Product = new Product { Name = "Nước suối" } }
            }
        };

        // [OOP - KẾ THỪA] Lớp con tối giản để kiểm tra hành vi mặc định của lớp cơ sở
        private sealed class TaiLieuThu : PrintableDocument
        {
            public TaiLieuThu(string tieuDe) : base(tieuDe) { }

            protected override string NoiDungChinh() => "<p>noi dung</p>";
        }

        private static int DemChuoi(string noiDung, string chuoiCan) =>
            noiDung.Split(chuoiCan, StringSplitOptions.None).Length - 1;

        [Fact]
        public void PhieuLuong_HienThiThucLinhDungDinhDang()
        {
            var nv = NhanVienMau();
            var doc = new PhieuLuongPrintable(KyMau(), PhieuMau(nv, 9_000_000m), nv);

            var html = WebUtility.HtmlDecode(doc.RenderHtml());

            Assert.Contains("9.000.000 đ", html);
            Assert.Contains("Thực lĩnh", html);
        }

        [Fact]
        public void DuLieuNguoiDung_DuocMaHoa_KhongChenScript()
        {
            // [OOP - ĐÓNG GÓI] Lớp con không cần tự mã hóa: lớp cơ sở làm việc đó
            var nv = NhanVienMau("<script>alert(1)</script>Kẻ xấu");
            var doc = new PhieuLuongPrintable(KyMau(), PhieuMau(nv, 1m), nv);

            var html = doc.RenderHtml();

            Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void TemplateMethod_GiuDungThuTuDauNoiDungChan()
        {
            var nv = NhanVienMau();
            var html = WebUtility.HtmlDecode(new PhieuLuongPrintable(KyMau(), PhieuMau(nv, 9_000_000m), nv).RenderBody());

            int dau = html.IndexOf("<h1>", StringComparison.Ordinal);
            int noiDung = html.IndexOf("Thực lĩnh", StringComparison.Ordinal);
            int chan = html.IndexOf("Người lập phiếu", StringComparison.Ordinal);

            Assert.True(dau >= 0 && dau < noiDung && noiDung < chan);
        }

        [Fact]
        public void DaHinh_PrintServiceInNhieuLoaiPhieuTrongMotTrang()
        {
            var nv = NhanVienMau();
            var ky = KyMau();
            var phieu = PhieuMau(nv, 9_000_000m);

            // [OOP - ĐA HÌNH] Ba loại khác nhau nhưng cùng kiểu IPrintable
            IPrintable[] cacPhieu =
            {
                new PhieuLuongPrintable(ky, phieu, nv),
                new BangLuongPrintable(ky, new[] { phieu }),
                new HoaDonPrintable(DonMau())
            };

            var html = WebUtility.HtmlDecode(new PrintService().RenderBatch(cacPhieu));

            foreach (var doc in cacPhieu)
            {
                Assert.Contains(doc.TieuDe, html);
            }
            Assert.Equal(3, DemChuoi(html, "class=\"trang\""));
        }

        [Fact]
        public void BangLuong_TinhTongThucLinhTuCacPhieu()
        {
            var a = NhanVienMau("Nhân viên A", "NV001");
            var b = NhanVienMau("Nhân viên B", "NV002");
            b.Id = 2;
            var cacPhieu = new[] { PhieuMau(a, 9_000_000m), PhieuMau(b, 4_500_000m) };

            var html = WebUtility.HtmlDecode(new BangLuongPrintable(KyMau(), cacPhieu).RenderHtml());

            Assert.Contains("13.500.000 đ", html);
        }

        [Fact]
        public void HoaDon_TinhThanhTienVaThanhToan()
        {
            var html = WebUtility.HtmlDecode(new HoaDonPrintable(DonMau()).RenderHtml());

            Assert.Contains("100.000 đ", html); // 2 x 50.000
            Assert.Contains("90.000 đ", html);  // Thanh toán sau giảm giá
        }

        [Fact]
        public void LopTruuTuong_TuChoiTieuDeRong()
        {
            Assert.Throws<ArgumentException>(() => new TaiLieuThu("   "));
        }

        [Fact]
        public void LopTruuTuong_DongGoiTieuDeVaCoChanTrangMacDinh()
        {
            var doc = new TaiLieuThu("  Tài liệu  ");

            Assert.Equal("Tài liệu", doc.TieuDe);
            Assert.Contains("ghichu", WebUtility.HtmlDecode(doc.RenderHtml()));
        }
    }
}
