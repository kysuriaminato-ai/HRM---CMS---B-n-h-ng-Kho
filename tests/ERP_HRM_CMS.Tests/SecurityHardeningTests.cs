using System.Net;
using ERP_HRM_CMS.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ERP_HRM_CMS.Tests
{
    /// <summary>Kiểm thử tích hợp cho các thay đổi bảo mật (chạy trên database ERP_HRM_CMS_TEST).</summary>
    public class SecurityHardeningTests : IClassFixture<TestAppFactory>
    {
        private readonly TestAppFactory _factory;

        public SecurityHardeningTests(TestAppFactory factory) => _factory = factory;

        [Fact]
        public async Task TrangDangNhap_CoHeaderBaoMat()
        {
            using var client = TestHelpers.NewClient(_factory);

            var response = await client.GetAsync("/Auth/Login");

            Assert.True(response.Headers.Contains("Content-Security-Policy"));
            Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
            Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        }

        [Fact]
        public async Task PhienCu_SauKhiSecurityStampDoi_BiThuHoi()
        {
            var userId = await TestHelpers.CreateUserAsync(_factory.Services);
            var username = await TestHelpers.GetUsernameAsync(_factory.Services, userId);
            using var client = TestHelpers.NewClient(_factory);

            var login = await TestHelpers.LoginAsync(client, username, TestHelpers.Password);
            Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);

            var truocKhi = await client.GetAsync("/Home/Index");
            Assert.DoesNotContain("/Auth/Login", truocKhi.Headers.Location?.ToString() ?? string.Empty);

            // Mô phỏng quản trị viên đổi quyền/mật khẩu: SecurityStamp thay đổi trong DB
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var user = await db.Users.FindAsync(userId);
                user!.SecurityStamp = Guid.NewGuid();
                await db.SaveChangesAsync();
            }

            var sauKhi = await client.GetAsync("/Home/Index");
            Assert.Equal(HttpStatusCode.Redirect, sauKhi.StatusCode);
            Assert.Contains("/Auth/Login", sauKhi.Headers.Location?.ToString() ?? string.Empty);
        }

        [Fact]
        public async Task PhienCu_KhiTaiKhoanBiKhoa_BiThuHoi()
        {
            var userId = await TestHelpers.CreateUserAsync(_factory.Services);
            var username = await TestHelpers.GetUsernameAsync(_factory.Services, userId);
            using var client = TestHelpers.NewClient(_factory);

            await TestHelpers.LoginAsync(client, username, TestHelpers.Password);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var user = await db.Users.FindAsync(userId);
                user!.IsActive = false;
                user.SecurityStamp = Guid.NewGuid();
                await db.SaveChangesAsync();
            }

            var response = await client.GetAsync("/Home/Index");
            Assert.Contains("/Auth/Login", response.Headers.Location?.ToString() ?? string.Empty);
        }

        [Fact]
        public async Task DangKy_TenDaTonTaiVaTenMoi_TraVeCungPhanHoi()
        {
            var userId = await TestHelpers.CreateUserAsync(_factory.Services);
            var tonTai = await TestHelpers.GetUsernameAsync(_factory.Services, userId);
            using var client = TestHelpers.NewClient(_factory);

            var voiTenTonTai = await DangKyAsync(client, tonTai);
            var voiTenMoi = await DangKyAsync(client, "moi_" + Guid.NewGuid().ToString("N")[..10]);

            // Không để lộ tên đăng nhập đã tồn tại: cùng mã trạng thái và cùng đích chuyển hướng
            Assert.Equal(voiTenMoi.StatusCode, voiTenTonTai.StatusCode);
            Assert.Equal(voiTenMoi.Headers.Location?.ToString(), voiTenTonTai.Headers.Location?.ToString());
        }

        private static async Task<HttpResponseMessage> DangKyAsync(HttpClient client, string username)
        {
            var token = await TestHelpers.GetAntiForgeryTokenAsync(client, "/Auth/Register");
            var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Username"] = username,
                ["Password"] = "Abcd1234",
                ["ConfirmPassword"] = "Abcd1234",
                ["__RequestVerificationToken"] = token
            });
            return await client.PostAsync("/Auth/Register", form);
        }
    }
}
