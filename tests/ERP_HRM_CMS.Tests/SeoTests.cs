using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace ERP_HRM_CMS.Tests
{
    public class SeoTests : IClassFixture<TestAppFactory>
    {
        private readonly TestAppFactory _factory;

        public SeoTests(TestAppFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Get_Home_Returns_200_And_Contains_OpenGraph_JsonLd_And_ImageDimensions()
        {
            using var client = TestHelpers.NewClient(_factory);

            using var response = await client.GetAsync("/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string html = await response.Content.ReadAsStringAsync();

            // Open Graph & Twitter Card
            Assert.Contains("og:title", html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("og:description", html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("og:type", html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("og:url", html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("og:image", html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("twitter:card", html, StringComparison.OrdinalIgnoreCase);

            // JSON-LD Structured Data (Organization & WebSite with SearchAction)
            Assert.Contains("application/ld+json", html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("\"Organization\"", html, StringComparison.Ordinal);
            Assert.Contains("\"WebSite\"", html, StringComparison.Ordinal);
            Assert.Contains("\"SearchAction\"", html, StringComparison.Ordinal);

            // Thẻ img trên trang chủ có thuộc tính width và height
            var imgMatches = Regex.Matches(html, @"<img\b[^>]*>", RegexOptions.IgnoreCase);
            Assert.NotEmpty(imgMatches);
            foreach (Match img in imgMatches)
            {
                Assert.Contains("width=", img.Value, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("height=", img.Value, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public async Task Get_RobotsTxt_Contains_Disallow_For_Admin_And_Staff()
        {
            using var client = TestHelpers.NewClient(_factory);

            using var response = await client.GetAsync("/robots.txt");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Disallow: /Admin", content, StringComparison.Ordinal);
            Assert.Contains("Disallow: /Staff", content, StringComparison.Ordinal);
            Assert.Contains("Disallow: /ThanhToan", content, StringComparison.Ordinal);
        }

        [Fact]
        public async Task Get_SitemapXml_Does_Not_Contain_Admin_Or_Staff()
        {
            using var client = TestHelpers.NewClient(_factory);

            using var response = await client.GetAsync("/sitemap.xml");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string xml = await response.Content.ReadAsStringAsync();
            Assert.Contains("<urlset", xml, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("/Admin", xml, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("/Staff", xml, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("/ThanhToan", xml, StringComparison.OrdinalIgnoreCase);
        }
    }
}

