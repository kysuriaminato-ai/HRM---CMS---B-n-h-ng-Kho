using System.Net;
using Xunit;

namespace ERP_HRM_CMS.Tests
{
    public class SecurityTests : IClassFixture<TestAppFactory>
    {
        private readonly TestAppFactory _factory;

        public SecurityTests(TestAppFactory factory) => _factory = factory;

        [Fact]
        public async Task Dashboard_Anonymous_RedirectsToLogin()
        {
            using var client = TestHelpers.NewClient(_factory);
            client.DefaultRequestHeaders.Accept.ParseAdd("text/html");

            var response = await client.GetAsync("/Home/Index");

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/Auth/Login", response.Headers.Location?.ToString() ?? string.Empty);
        }

        [Fact]
        public async Task NotificationHub_Negotiate_Anonymous_IsRejected()
        {
            using var client = TestHelpers.NewClient(_factory);

            var response = await client.PostAsync("/hub/notifications/negotiate?negotiateVersion=1", null);

            Assert.False(response.IsSuccessStatusCode);
        }
    }
}
