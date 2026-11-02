using ERP_HRM_CMS.Helpers;
using Xunit;

namespace ERP_HRM_CMS.Tests
{
    public class HtmlSanitizerTests
    {
        [Fact]
        public void Script_Tag_IsRemovedWithContent()
        {
            var result = HtmlSanitizerHelper.Sanitize("<p>Xin chào</p><script>alert(1)</script>");

            Assert.Contains("<p>Xin chào</p>", result);
            Assert.DoesNotContain("script", result, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("alert", result);
        }

        [Fact]
        public void EventHandlerAttributes_AreRemoved()
        {
            var result = HtmlSanitizerHelper.Sanitize("<img src=\"/images/a.svg\" onerror=\"alert(1)\">");

            Assert.Contains("src=\"/images/a.svg\"", result);
            Assert.DoesNotContain("onerror", result, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("<a href=\"javascript:alert(1)\">x</a>")]
        [InlineData("<a href=\"&#106;avascript:alert(1)\">x</a>")]
        [InlineData("<a href=\"java\tscript:alert(1)\">x</a>")]
        [InlineData("<a href=\"//evil.example.com\">x</a>")]
        [InlineData("<a href=\"data:text/html;base64,PHNjcmlwdD4=\">x</a>")]
        public void UnsafeLinks_LoseHref(string input)
        {
            var result = HtmlSanitizerHelper.Sanitize(input);

            Assert.DoesNotContain("href", result);
            Assert.DoesNotContain("javascript", result, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(">x</a>", result);
        }

        [Fact]
        public void SafeFormatting_IsKept()
        {
            var input = "<p><strong>Đậm</strong> và <a href=\"https://example.com/bai\" title=\"Xem\">liên kết</a></p>";

            var result = HtmlSanitizerHelper.Sanitize(input);

            Assert.Contains("<strong>Đậm</strong>", result);
            Assert.Contains("href=\"https://example.com/bai\"", result);
            Assert.Contains("title=\"Xem\"", result);
        }

        [Fact]
        public void IframeAndObject_AreRemoved()
        {
            var result = HtmlSanitizerHelper.Sanitize(
                "<iframe src=\"https://evil.example\"></iframe><object data=\"x\">y</object>Hết");

            Assert.DoesNotContain("iframe", result, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("object", result, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Hết", result);
        }

        [Fact]
        public void StrayAngleBrackets_AreEncoded_AndUnknownTagsDropped()
        {
            var result = HtmlSanitizerHelper.Sanitize("Giá a < b và <unknown>");

            Assert.Contains("a &lt; b", result);
            Assert.DoesNotContain("unknown", result);
        }

        [Fact]
        public void NullOrEmpty_ReturnsEmptyString()
        {
            Assert.Equal(string.Empty, HtmlSanitizerHelper.Sanitize(null));
            Assert.Equal(string.Empty, HtmlSanitizerHelper.Sanitize(""));
        }
    }
}
