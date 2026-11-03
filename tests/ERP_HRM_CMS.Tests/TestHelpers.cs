using System.Text.RegularExpressions;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ERP_HRM_CMS.Tests
{
    public static class TestHelpers
    {
        public const string Password = "Test@12345";

        /// <summary>Tạo người dùng mới (tên ngẫu nhiên) và trả về Id.</summary>
        public static async Task<int> CreateUserAsync(IServiceProvider services, bool isActive = true)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var salt = MatKhauHelper.GenerateSalt();
            var user = new User
            {
                Username = "t_" + Guid.NewGuid().ToString("N")[..12],
                PasswordSalt = salt,
                PasswordHash = MatKhauHelper.HashPassword(Password, salt),
                IsActive = isActive,
                IsSuperAdmin = false,
                CreatedAt = VietnamTime.Now
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return user.Id;
        }

        public static async Task<string> GetUsernameAsync(IServiceProvider services, int userId)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return (await db.Users.FindAsync(userId))!.Username;
        }

        /// <summary>Client không tự chuyển hướng để kiểm tra được mã trả về và header.</summary>
        public static HttpClient NewClient(WebApplicationFactory<Program> factory) =>
            factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        public static async Task<string> GetAntiForgeryTokenAsync(HttpClient client, string path)
        {
            var html = await client.GetStringAsync(path);
            var match = Regex.Match(html, "__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
            if (!match.Success) throw new InvalidOperationException("Không tìm thấy token chống CSRF trong " + path);
            return match.Groups[1].Value;
        }

        public static async Task<HttpResponseMessage> LoginAsync(HttpClient client, string username, string password)
        {
            var token = await GetAntiForgeryTokenAsync(client, "/Auth/Login");
            var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Username"] = username,
                ["Password"] = password,
                ["RememberMe"] = "false",
                ["__RequestVerificationToken"] = token
            });
            return await client.PostAsync("/Auth/Login", form);
        }
    }
}
