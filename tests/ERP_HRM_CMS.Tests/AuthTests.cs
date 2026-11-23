using System.Net;
using System.Security.Cryptography;
using System.Text;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Services.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ERP_HRM_CMS.Tests
{
    public class AuthTests : IClassFixture<TestAppFactory>
    {
        private readonly TestAppFactory _factory;

        public AuthTests(TestAppFactory factory) => _factory = factory;

        [Fact]
        public async Task Login_WithCorrectPassword_RedirectsAndSetsAuthCookie()
        {
            var userId = await TestHelpers.CreateUserAsync(_factory.Services);
            var username = await TestHelpers.GetUsernameAsync(_factory.Services, userId);
            using var client = TestHelpers.NewClient(_factory);

            var response = await TestHelpers.LoginAsync(client, username, TestHelpers.Password);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.True(response.Headers.Contains("Set-Cookie"));
        }

        [Fact]
        public async Task Login_WithWrongPassword_ShowsError()
        {
            var userId = await TestHelpers.CreateUserAsync(_factory.Services);
            var username = await TestHelpers.GetUsernameAsync(_factory.Services, userId);
            using var client = TestHelpers.NewClient(_factory);

            var response = await TestHelpers.LoginAsync(client, username, "Sai@999999");
            var body = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("không chính xác", body);
        }

        [Fact]
        public async Task Login_Inactive_Account_IsRejected()
        {
            var userId = await TestHelpers.CreateUserAsync(_factory.Services, isActive: false);
            var username = await TestHelpers.GetUsernameAsync(_factory.Services, userId);
            using var client = TestHelpers.NewClient(_factory);

            var response = await TestHelpers.LoginAsync(client, username, TestHelpers.Password);
            var body = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("chờ duyệt", body);
        }

        [Fact]
        public async Task Login_AfterFiveWrongAttempts_IsLockedEvenWithCorrectPassword()
        {
            var userId = await TestHelpers.CreateUserAsync(_factory.Services);
            var username = await TestHelpers.GetUsernameAsync(_factory.Services, userId);
            using var client = TestHelpers.NewClient(_factory);

            for (int i = 0; i < 5; i++)
            {
                await TestHelpers.LoginAsync(client, username, "Sai@999999");
            }

            var response = await TestHelpers.LoginAsync(client, username, TestHelpers.Password);
            var body = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("sai quá nhiều lần", body);
        }

        [Fact]
        public void LegacyCompatiblePasswordHasher_Verifies_LegacyPbkdf2_And_Returns_SuccessRehashNeeded()
        {
            var hasher = new LegacyCompatiblePasswordHasher();
            var salt = MatKhauHelper.GenerateSalt();
            var legacyPbkdf2Hash = MatKhauHelper.HashPassword("MatKhauCu@123", salt);
            var user = new User { Username = "u_pbkdf2", PasswordSalt = salt, PasswordHash = legacyPbkdf2Hash };

            var okResult = hasher.VerifyHashedPassword(user, legacyPbkdf2Hash, "MatKhauCu@123");
            var failResult = hasher.VerifyHashedPassword(user, legacyPbkdf2Hash, "SaiMatKhau@123");

            Assert.Equal(PasswordVerificationResult.SuccessRehashNeeded, okResult);
            Assert.Equal(PasswordVerificationResult.Failed, failResult);
        }

        [Fact]
        public void LegacyCompatiblePasswordHasher_Verifies_LegacySha256Hex_And_Returns_SuccessRehashNeeded()
        {
            var hasher = new LegacyCompatiblePasswordHasher();
            var salt = "salt_sha256";
            var sha256Hex = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("MatKhauSha256@123" + salt)));
            var user = new User { Username = "u_sha256", PasswordSalt = salt, PasswordHash = sha256Hex };

            var okResult = hasher.VerifyHashedPassword(user, sha256Hex, "MatKhauSha256@123");
            var failResult = hasher.VerifyHashedPassword(user, sha256Hex, "SaiMatKhau@123");

            Assert.Equal(PasswordVerificationResult.SuccessRehashNeeded, okResult);
            Assert.Equal(PasswordVerificationResult.Failed, failResult);
        }

        [Fact]
        public void LegacyCompatiblePasswordHasher_Hashes_New_IdentityV3_And_Returns_Success()
        {
            var hasher = new LegacyCompatiblePasswordHasher();
            var user = new User { Username = "u_identity_v3" };
            var newHash = hasher.HashPassword(user, "MatKhauMoi@123");

            Assert.True(LegacyCompatiblePasswordHasher.IsIdentityFormatHash(newHash));
            Assert.False(string.IsNullOrEmpty(user.PasswordSalt));
            Assert.Equal(PasswordVerificationResult.Success, hasher.VerifyHashedPassword(user, newHash, "MatKhauMoi@123"));
            Assert.Equal(PasswordVerificationResult.Failed, hasher.VerifyHashedPassword(user, newHash, "SaiMatKhau@123"));
        }

        [Fact]
        public async Task Login_WithLegacyHash_UpgradesToIdentityV3HashInDatabase_While_WrongPassword_DoesNotChangeHash()
        {
            var userId = await TestHelpers.CreateUserAsync(_factory.Services);
            var username = await TestHelpers.GetUsernameAsync(_factory.Services, userId);

            string hashBefore;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                hashBefore = (await db.Users.AsNoTracking().FirstAsync(u => u.Id == userId)).PasswordHash;
            }
            Assert.False(LegacyCompatiblePasswordHasher.IsIdentityFormatHash(hashBefore));

            using var client = TestHelpers.NewClient(_factory);

            // 1. Đăng nhập sai mật khẩu (kể cả nhập mật khẩu mặc định cũ "123456" hay "Admin@123456") -> Hash trong DB tuyệt đối không đổi
            var wrongRes = await TestHelpers.LoginAsync(client, username, "Admin@123456");
            Assert.Equal(HttpStatusCode.OK, wrongRes.StatusCode);
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var hashAfterWrong = (await db.Users.AsNoTracking().FirstAsync(u => u.Id == userId)).PasswordHash;
                Assert.Equal(hashBefore, hashAfterWrong);
            }

            // 2. Đăng nhập đúng mật khẩu -> Hash trong DB được nâng cấp sang chuẩn Identity v3 (prefix AQAAAAIA, độ dài 84)
            var response = await TestHelpers.LoginAsync(client, username, TestHelpers.Password);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var hashAfter = (await db.Users.AsNoTracking().FirstAsync(u => u.Id == userId)).PasswordHash;
                Assert.NotEqual(hashBefore, hashAfter);
                Assert.StartsWith("AQAAAAIA", hashAfter, StringComparison.Ordinal);
                Assert.Equal(84, hashAfter.Length);
                Assert.True(LegacyCompatiblePasswordHasher.IsIdentityFormatHash(hashAfter));
            }
        }

        [Fact]
        public async Task SeedDemo_DoesNotOverwrite_ExistingUser_PasswordHash()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var admin = await db.Users.FirstAsync(u => u.Username == "admin");
            const string customHash = "AQAAAAIAAYagAAAAECustomAdminHashPreservedAcrossSeedDemoTestValue1234567890==";
            string originalHash = admin.PasswordHash;

            try
            {
                admin.PasswordHash = customHash;
                await db.SaveChangesAsync();

                // Chạy lại SeedAsync với seedDemo = true
                await DbInitializer.SeedAsync(db, TestAppFactory.AdminPasswordForTests, seedDemo: true);

                var reloadedAdmin = await db.Users.AsNoTracking().FirstAsync(u => u.Username == "admin");
                Assert.Equal(customHash, reloadedAdmin.PasswordHash);
            }
            finally
            {
                var restoreAdmin = await db.Users.FirstAsync(u => u.Username == "admin");
                restoreAdmin.PasswordHash = originalHash;
                await db.SaveChangesAsync();
            }
        }

        [Fact]
        public async Task UserManager_FindByNameAsync_Resolves_Mapped_User_Without_Warning()
        {
            var userId = await TestHelpers.CreateUserAsync(_factory.Services);
            var username = await TestHelpers.GetUsernameAsync(_factory.Services, userId);

            using var scope = _factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

            var found = await userManager.FindByNameAsync(username);

            Assert.NotNull(found);
            Assert.Equal(userId, found!.Id);
            Assert.Equal(username, found.Username);
            Assert.Equal(username, found.UserName);
            Assert.Equal(username.ToUpperInvariant(), found.NormalizedUserName);
        }

        [Fact]
        public async Task Public_Register_Assigns_Customer_Role_Activates_Immediately_And_Blocks_Admin_Pages()
        {
            var username = "cust_" + Guid.NewGuid().ToString("N")[..10];
            using var client = TestHelpers.NewClient(_factory);

            var regToken = await TestHelpers.GetAntiForgeryTokenAsync(client, "/Auth/Register");
            var regForm = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Username"] = username,
                ["Password"] = "KhachHang@123",
                ["ConfirmPassword"] = "KhachHang@123",
                ["__RequestVerificationToken"] = regToken
            });
            var regRes = await client.PostAsync("/Auth/Register", regForm);
            Assert.Equal(HttpStatusCode.Redirect, regRes.StatusCode);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var user = await db.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Username == username);

                Assert.NotNull(user);
                Assert.True(user!.IsActive);
                Assert.False(user.IsSuperAdmin);
                var roleCodes = user.UserRoles.Select(ur => ur.Role.Code).ToList();
                Assert.Contains("CUSTOMER", roleCodes);
                Assert.DoesNotContain("EMPLOYEE", roleCodes);
            }

            // Đăng nhập bằng tài khoản CUSTOMER vừa tạo -> Redirect về trang chủ cửa hàng (/)
            var loginRes = await TestHelpers.LoginAsync(client, username, "KhachHang@123");
            Assert.Equal(HttpStatusCode.Redirect, loginRes.StatusCode);
            Assert.Equal("/", loginRes.Headers.Location?.ToString());

            // Khách hàng cố truy cập trang quản trị Dashboard (/Home/Index) hoặc Quản lý nhân sự (/NhanSu/Index) bị chặn
            var adminHomeRes = await client.GetAsync("/Home/Index");
            Assert.Equal(HttpStatusCode.Redirect, adminHomeRes.StatusCode);
            Assert.Contains("KhongCoQuyen", adminHomeRes.Headers.Location?.ToString() ?? string.Empty);

            var adminHrRes = await client.GetAsync("/NhanSu/Index");
            Assert.Equal(HttpStatusCode.Redirect, adminHrRes.StatusCode);
            Assert.Contains("KhongCoQuyen", adminHrRes.Headers.Location?.ToString() ?? string.Empty);
        }
    }
}
