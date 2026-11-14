using System.Net;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ERP_HRM_CMS.Tests
{
    public class ChamCongIndexTests : IClassFixture<TestAppFactory>
    {
        private readonly TestAppFactory _factory;

        public ChamCongIndexTests(TestAppFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Get_ChamCong_Index_With_AttendanceViewPermission_Returns_200_Ok()
        {
            var username = $"att_view_{Guid.NewGuid():N}"[..16];
            const string password = "UserTest@12345";

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

                var role = await db.Roles.FirstAsync(r => r.Code == "ADMIN_NHANVIEN");
                var perm = await db.Permissions.FirstAsync(p => p.Code == "hrm.attendance.view");

                var user = new User
                {
                    Username = username,
                    PasswordSalt = MatKhauHelper.GenerateSalt(),
                    IsActive = true,
                    IsSuperAdmin = false,
                    MustChangePassword = false,
                    CreatedAt = VietnamTime.Now
                };
                user.PasswordHash = hasher.HashPassword(user, password);
                user.UserRoles.Add(new UserRole { RoleId = role.Id });
                user.UserPermissions.Add(new UserPermission { PermissionId = perm.Id, IsGranted = true });

                db.Users.Add(user);
                await db.SaveChangesAsync();
            }

            using var client = TestHelpers.NewClient(_factory);
            var loginRes = await TestHelpers.LoginAsync(client, username, password);
            Assert.Equal(HttpStatusCode.Redirect, loginRes.StatusCode);

            var exception = await Record.ExceptionAsync(async () =>
            {
                using var res = await client.GetAsync("/ChamCong/Index");
                Assert.Equal(HttpStatusCode.OK, res.StatusCode);

                var html = await res.Content.ReadAsStringAsync();

                // TagHelper erp-page-header xuất tiêu đề qua WebUtility.HtmlEncode,
                // hàm này mã hoá ký tự tiếng Việt thành dạng số (B&#7843;ng...).
                // Giải mã lại trước khi so sánh để khỏi phụ thuộc cách mã hoá.
                var plain = System.Net.WebUtility.HtmlDecode(html);
                Assert.Contains("Bảng Chấm Công Hàng Ngày", plain);
            });

            Assert.Null(exception);
        }
    }
}

