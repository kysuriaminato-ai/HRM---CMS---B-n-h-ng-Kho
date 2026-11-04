using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.ViewModels;
using ERP_HRM_CMS.Services.Security;
using Xunit;

namespace ERP_HRM_CMS.Tests
{
    /// <summary>Kiểm thử các quy tắc bảo mật thuần (không cần database).</summary>
    public class PermissionDelegationGuardTests
    {
        private static IReadOnlySet<string> Quyen(params string[] codes) =>
            new HashSet<string>(codes, StringComparer.OrdinalIgnoreCase);

        [Fact]
        public void NguoiKhongCoQuyen_KhongDuocGanQuyenMaKhongCo()
        {
            var thayDoi = new[] { (1, "hrm.payroll.view") };

            var loi = PermissionDelegationGuard.KiemTra(thayDoi, Quyen("menu.view"), laSuperAdmin: false);

            Assert.Equal(PermissionDelegationGuard.ThongBaoKhongDuQuyen, loi);
        }

        [Fact]
        public void NguoiCoQuyen_DuocGanHoacThuHoiQuyenDo()
        {
            var thayDoi = new[] { (1, "menu.view") };

            Assert.Null(PermissionDelegationGuard.KiemTra(thayDoi, Quyen("menu.view"), laSuperAdmin: false));
        }

        [Fact]
        public void QuyenAuth_KhongPhaiSuperAdmin_BiTuChoiDuDangCoQuyenDo()
        {
            var thayDoi = new[] { (7, "auth.user.manage") };

            var loi = PermissionDelegationGuard.KiemTra(
                thayDoi, Quyen("auth.user.manage", "auth.matrix.update"), laSuperAdmin: false);

            Assert.Equal(PermissionDelegationGuard.ThongBaoAuthChiSuperAdmin, loi);
        }

        [Fact]
        public void SuperAdmin_DuocMoiThayDoi()
        {
            var thayDoi = new[] { (7, "auth.user.manage"), (8, "hrm.payroll.view") };

            Assert.Null(PermissionDelegationGuard.KiemTra(thayDoi, Quyen(), laSuperAdmin: true));
        }

        [Fact]
        public void KhongCoThayDoi_LuonDuocPhep()
        {
            Assert.Null(PermissionDelegationGuard.KiemTra(Array.Empty<(int, string)>(), Quyen(), laSuperAdmin: false));
        }
    }

    public class UrlAnToanTests
    {
        [Theory]
        [InlineData("/gioi-thieu")]
        [InlineData("https://example.com/trang")]
        [InlineData("http://example.com")]
        [InlineData("mailto:lienhe@example.com")]
        [InlineData("")]
        [InlineData(null)]
        public void DuongDanAnToan_DuocChapNhan(string? url)
        {
            Assert.True(UrlAnToan.LaHopLe(url));
        }

        [Theory]
        [InlineData("javascript:alert(1)")]
        [InlineData("JavaScript:alert(1)")]
        [InlineData("  javascript:alert(1)")]
        [InlineData("data:text/html;base64,PHNjcmlwdD4=")]
        [InlineData("vbscript:msgbox")]
        [InlineData("java\tscript:alert(1)")]
        [InlineData("//evil.example.com")]
        [InlineData("/\\evil.example.com")]
        [InlineData("ftp://example.com")]
        [InlineData("/co\u0007ky-tu-dieu-khien")]
        public void DuongDanNguyHiem_BiTuChoi(string url)
        {
            Assert.False(UrlAnToan.LaHopLe(url));
        }

        [Fact]
        public void Attribute_ApDungCungQuyTacVoiDataAnnotations()
        {
            var attr = new UrlAnToanAttribute();

            Assert.True(attr.IsValid("/bai-viet/abc"));
            Assert.False(attr.IsValid("javascript:1"));
        }
    }

    public class MaChungTuTests
    {
        [Fact]
        public void Sinh_DungDinhDangVaDoDaiToiDa()
        {
            var moc = new DateTime(2026, 11, 4, 14, 12, 0);

            string ma = MaChungTu.Sinh("PNK", moc);

            Assert.Matches(@"^PNK-20261104141200-[0-9A-F]{8}$", ma);
            Assert.True(ma.Length <= 50, "Mã phải vừa cột MaxLength(50) của TransactionCode/OrderCode/SKU.");
        }

        [Fact]
        public void Sinh_TaoMaKhacNhauLienTiep()
        {
            var tapHop = new HashSet<string>();
            for (int i = 0; i < 1000; i++)
            {
                tapHop.Add(MaChungTu.Sinh("ORD"));
            }

            Assert.Equal(1000, tapHop.Count);
        }

        [Fact]
        public async Task SinhDuyNhat_BoQuaMaDaTonTaiVaThuLai()
        {
            int lan = 0;

            string ma = await MaChungTu.SinhDuyNhatAsync("HDLD", m =>
            {
                lan++;
                // Hai lần đầu coi như đã tồn tại
                return Task.FromResult(lan <= 2);
            });

            Assert.Equal(3, lan);
            Assert.StartsWith("HDLD-", ma);
        }

        [Fact]
        public async Task SinhDuyNhat_NemLoiKhiHetSoLanThu()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                MaChungTu.SinhDuyNhatAsync("SKU", _ => Task.FromResult(true)));
        }
    }

    /// <summary>Chống mass-assignment: ViewModel chỉ ghi các trường được phép vào entity đã tải.</summary>
    public class MenuContentMappingTests
    {
        [Fact]
        public void MenuFormViewModel_KhongGhiDeIdVaCreatedAt()
        {
            var taoLuc = new DateTime(2024, 1, 1);
            var menu = new Menu { Id = 5, Title = "Cũ", CreatedAt = taoLuc, Target = "_self" };
            var vm = new MenuFormViewModel
            {
                Id = 999,
                Title = "  Mới  ",
                Target = "_blank",
                CustomUrl = "/gioi-thieu",
                Position = "MainNav",
                LinkType = "External",
                IsActive = true
            };

            vm.GhiVaoEntity(menu);

            Assert.Equal(5, menu.Id);
            Assert.Equal(taoLuc, menu.CreatedAt);
            Assert.Equal("Mới", menu.Title);
            Assert.Equal("_blank", menu.Target);
            Assert.Equal("/gioi-thieu", menu.CustomUrl);
        }

        [Fact]
        public void ContentFormViewModel_KhongGhiDeIdVaTacGia()
        {
            var content = new Content { Id = 3, AuthorId = 11, Slug = "cu", CreatedAt = new DateTime(2024, 1, 1) };
            var vm = new ContentFormViewModel
            {
                Id = 77,
                Title = "Tiêu đề",
                Slug = "moi",
                BodyHtml = "<p>Nội dung</p>",
                Status = "Published"
            };

            vm.GhiVaoEntity(content);

            Assert.Equal(3, content.Id);
            Assert.Equal(11, content.AuthorId);
            Assert.Equal("moi", content.Slug);
            Assert.Equal("Tiêu đề", content.Title);
        }
    }
}
