using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ERP_HRM_CMS.Tests
{
    public class LoyaltyTierTests : IClassFixture<TestAppFactory>
    {
        private readonly TestAppFactory _factory;

        public LoyaltyTierTests(TestAppFactory factory)
        {
            _factory = factory;
        }

        [Theory]
        [InlineData(0, "SILVER", "Bạc", 50000000)]
        [InlineData(49999999, "SILVER", "Bạc", 50000000)]
        [InlineData(50000000, "GOLD", "Vàng", 200000000)]
        [InlineData(199999999, "GOLD", "Vàng", 200000000)]
        [InlineData(200000000, "PLATINUM", "Bạch Kim", 500000000)]
        [InlineData(499999999, "PLATINUM", "Bạch Kim", 500000000)]
        [InlineData(500000000, "DIAMOND", "Kim Cương", 0)]
        [InlineData(1000000000, "DIAMOND", "Kim Cương", 0)]
        public void LoyaltyHelper_Returns_Correct_Tier_At_Boundary_Values(
            decimal revenue,
            string expectedCode,
            string expectedName,
            decimal expectedNextMilestone)
        {
            var tier = LoyaltyHelper.GetTier(revenue);

            Assert.NotNull(tier);
            Assert.Equal(expectedCode, tier.Code);
            Assert.Equal(expectedName, tier.Name);
            Assert.Equal(expectedNextMilestone, tier.NextMilestone);

            if (expectedNextMilestone == 0)
            {
                Assert.Equal(100m, tier.ProgressPercent);
            }
            else
            {
                Assert.InRange(tier.ProgressPercent, 0m, 100m);
            }
        }

        [Fact]
        public async Task User_Cannot_Access_Other_Customer_Loyalty_Tier_Or_Revenue_IDOR_Prevented()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
            var customerRole = await db.Roles.FirstAsync(r => r.Code == "CUSTOMER");

            string emailA = $"customer_a_{Guid.NewGuid():N}@aura-example.vn";
            string nameA = $"Nguyễn Văn A {Guid.NewGuid():N}"[..20];
            string emailB = $"customer_b_{Guid.NewGuid():N}@aura-example.vn";
            string nameB = $"Trần Thị B {Guid.NewGuid():N}"[..20];

            // 1. Tạo User A & Customer A (hạng GOLD: 60.000.000đ)
            string usernameA = "user_a_" + Guid.NewGuid().ToString("N")[..8];
            var saltA = MatKhauHelper.GenerateSalt();
            var userA = new User
            {
                Username = usernameA,
                Email = emailA,
                NormalizedEmail = emailA.ToUpperInvariant(),
                PasswordSalt = saltA,
                IsActive = true,
                MustChangePassword = false,
                CreatedAt = VietnamTime.Now
            };
            userA.PasswordHash = hasher.HashPassword(userA, TestHelpers.Password);
            userA.UserRoles.Add(new UserRole { RoleId = customerRole.Id });
            db.Users.Add(userA);

            var customerA = new Customer
            {
                Code = "KH-A-" + Guid.NewGuid().ToString("N")[..6],
                FullName = nameA,
                Email = emailA,
                TotalRevenue = 60_000_000m, // Hạng GOLD
                IsActive = true,
                IsDeleted = false,
                CreatedAt = VietnamTime.Now
            };
            db.Customers.Add(customerA);

            // 2. Tạo User B & Customer B (hạng PLATINUM: 350.000.000đ)
            string usernameB = "user_b_" + Guid.NewGuid().ToString("N")[..8];
            var saltB = MatKhauHelper.GenerateSalt();
            var userB = new User
            {
                Username = usernameB,
                Email = emailB,
                NormalizedEmail = emailB.ToUpperInvariant(),
                PasswordSalt = saltB,
                IsActive = true,
                MustChangePassword = false,
                CreatedAt = VietnamTime.Now
            };
            userB.PasswordHash = hasher.HashPassword(userB, TestHelpers.Password);
            userB.UserRoles.Add(new UserRole { RoleId = customerRole.Id });
            db.Users.Add(userB);

            var customerB = new Customer
            {
                Code = "KH-B-" + Guid.NewGuid().ToString("N")[..6],
                FullName = nameB,
                Email = emailB,
                TotalRevenue = 350_000_000m, // Hạng PLATINUM
                IsActive = true,
                IsDeleted = false,
                CreatedAt = VietnamTime.Now
            };
            db.Customers.Add(customerB);

            await db.SaveChangesAsync();

            // 3. User A đăng nhập
            using var client = TestHelpers.NewClient(_factory);
            var loginRes = await TestHelpers.LoginAsync(client, usernameA, TestHelpers.Password);
            Assert.Equal(HttpStatusCode.Redirect, loginRes.StatusCode);

            // 4. User A truy vấn trang chủ, cố tình truyền customerId / email của User B qua query string (thử tấn công IDOR)
            var maliciousUrl = $"/?customerId={customerB.Id}&email={Uri.EscapeDataString(emailB)}&userId={userB.Id}";
            var response = await client.GetAsync(maliciousUrl);
            var html = await response.Content.ReadAsStringAsync();
            var decodedHtml = System.Net.WebUtility.HtmlDecode(html);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Bắt buộc: Phải hiển thị dữ liệu của chính User A
            Assert.Contains(nameA.ToUpperInvariant(), decodedHtml);
            Assert.Contains("GOLD", decodedHtml);

            // Tuyệt đối không được lộ tên hay hạng của Customer B trong khối thẻ thành viên
            Assert.DoesNotContain(nameB.ToUpperInvariant(), decodedHtml);

            // Khối thẻ thành viên hiển thị hạng GOLD, không hiển thị thẻ PLATINUM của B
            Assert.DoesNotContain($"<div class=\"fw-bold\" style=\"color: #e5e4e2;\">PLATINUM</div>", html);
        }
    }
}

