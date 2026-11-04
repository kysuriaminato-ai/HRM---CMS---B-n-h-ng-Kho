using System.Net;
using Xunit;

namespace ERP_HRM_CMS.Tests
{
    /// <summary>Trang công khai phải hiển thị được với khách chưa đăng nhập và không đưa ra thông báo sai.</summary>
    public class PublicPagesTests : IClassFixture<TestAppFactory>
    {
        private readonly TestAppFactory _factory;

        public PublicPagesTests(TestAppFactory factory) => _factory = factory;

        [Theory]
        [InlineData("/")]
        [InlineData("/Public/TinTuc")]
        [InlineData("/Public/TinTuc?keyword=erp")]
        [InlineData("/Public/GioiThieu")]
        [InlineData("/Public/LienHe")]
        public async Task Public_Pages_Render_For_Anonymous_Visitors(string path)
        {
            using var client = TestHelpers.NewClient(_factory);

            var response = await client.GetAsync(path);
            var html = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("public-site.css", html);
            Assert.Contains("id=\"psSearchToggle\"", html);
            Assert.Contains("id=\"noi-dung-chinh\"", html);
        }

        [Fact]
        public async Task Contact_Form_Does_Not_Claim_A_Request_Was_Stored()
        {
            using var client = TestHelpers.NewClient(_factory);

            var html = await (await client.GetAsync("/Public/LienHe")).Content.ReadAsStringAsync();

            // Form chỉ mở ứng dụng email, không lưu yêu cầu: không được hiển thị thông báo "đã gửi" giả
            Assert.Contains("id=\"psContactForm\"", html);
            Assert.DoesNotContain("Cảm ơn bạn đã gửi liên hệ", html);
        }

        [Fact]
        public async Task Unknown_Article_Returns_NotFound()
        {
            using var client = TestHelpers.NewClient(_factory);

            var response = await client.GetAsync("/bai-viet/khong-ton-tai-" + Guid.NewGuid().ToString("N"));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
