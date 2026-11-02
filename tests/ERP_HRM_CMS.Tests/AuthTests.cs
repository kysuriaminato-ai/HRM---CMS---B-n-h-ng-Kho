using System.Net;
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
    }
}
