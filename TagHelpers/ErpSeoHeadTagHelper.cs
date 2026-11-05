using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.EntityFrameworkCore;

namespace ERP_HRM_CMS.TagHelpers
{
    [HtmlTargetElement("erp-seo-head")]
    public class ErpSeoHeadTagHelper : TagHelper
    {
        private readonly AppDbContext _context;

        public ErpSeoHeadTagHelper(AppDbContext context)
        {
            _context = context;
        }

        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext ViewContext { get; set; } = null!;

        [HtmlAttributeName("title")]
        public string? Title { get; set; }

        [HtmlAttributeName("description")]
        public string? Description { get; set; }

        [HtmlAttributeName("keywords")]
        public string? Keywords { get; set; }

        [HtmlAttributeName("canonical")]
        public string? Canonical { get; set; }

        [HtmlAttributeName("og-image")]
        public string? OgImage { get; set; }

        [HtmlAttributeName("og-type")]
        public string OgType { get; set; } = "website";

        [HtmlAttributeName("entity-type")]
        public string? EntityType { get; set; }

        [HtmlAttributeName("entity-id")]
        public int? EntityId { get; set; }

        [HtmlAttributeName("custom-json-ld")]
        public string? CustomJsonLd { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = null; // Do not output <erp-seo-head> tag itself

            var request = ViewContext.HttpContext.Request;
            string baseUrl = $"{request.Scheme}://{request.Host}";
            string currentPath = request.Path.Value ?? "/";

            // Đọc cài đặt trang web
            var settings = await _context.SiteSettings.AsNoTracking().ToDictionaryAsync(s => s.SettingKey, s => s.SettingValue ?? string.Empty);
            string siteName = settings.GetValueOrDefault("Site_Name") ?? "ERP Enterprise";

            // Nếu có EntityType & EntityId, tra cứu SeoMeta
            if (!string.IsNullOrWhiteSpace(EntityType) && EntityId.HasValue && EntityId.Value > 0)
            {
                var seo = await _context.SeoMetas.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.EntityType == EntityType && s.EntityId == EntityId.Value && !s.IsDeleted);

                if (seo != null)
                {
                    if (string.IsNullOrWhiteSpace(Title) && !string.IsNullOrWhiteSpace(seo.MetaTitle))
                        Title = seo.MetaTitle;

                    if (string.IsNullOrWhiteSpace(Description) && !string.IsNullOrWhiteSpace(seo.MetaDescription))
                        Description = seo.MetaDescription;

                    if (string.IsNullOrWhiteSpace(Keywords) && !string.IsNullOrWhiteSpace(seo.MetaKeywords))
                        Keywords = seo.MetaKeywords;

                    if (string.IsNullOrWhiteSpace(Canonical) && !string.IsNullOrWhiteSpace(seo.CanonicalUrl))
                        Canonical = seo.CanonicalUrl;

                    if (string.IsNullOrWhiteSpace(OgImage) && !string.IsNullOrWhiteSpace(seo.OgImage))
                        OgImage = seo.OgImage;

                    if (string.IsNullOrWhiteSpace(CustomJsonLd) && !string.IsNullOrWhiteSpace(seo.StructuredData))
                        CustomJsonLd = seo.StructuredData;
                }
            }

            string finalTitle = !string.IsNullOrWhiteSpace(Title) ? $"{Title} | {siteName}" : siteName;
            string finalDesc = !string.IsNullOrWhiteSpace(Description) ? Description : siteName;
            string finalCanonical = !string.IsNullOrWhiteSpace(Canonical)
                ? (Canonical.StartsWith("http") ? Canonical : $"{baseUrl}{Canonical}")
                : $"{baseUrl}{currentPath}";

            string finalOgImage = !string.IsNullOrWhiteSpace(OgImage)
                ? (OgImage.StartsWith("http") ? OgImage : $"{baseUrl}{OgImage}")
                : $"{baseUrl}/images/brand-og-share.png";

            output.Content.AppendHtml($"<meta name=\"description\" content=\"{HtmlEncoder.Default.Encode(finalDesc)}\" />\n");
            if (!string.IsNullOrWhiteSpace(Keywords))
            {
                output.Content.AppendHtml($"<meta name=\"keywords\" content=\"{HtmlEncoder.Default.Encode(Keywords)}\" />\n");
            }
            output.Content.AppendHtml("<meta name=\"robots\" content=\"index, follow\" />\n");
            output.Content.AppendHtml($"<link rel=\"canonical\" href=\"{HtmlEncoder.Default.Encode(finalCanonical)}\" />\n");

            // Open Graph
            output.Content.AppendHtml($"<meta property=\"og:site_name\" content=\"{HtmlEncoder.Default.Encode(siteName)}\" />\n");
            output.Content.AppendHtml($"<meta property=\"og:title\" content=\"{HtmlEncoder.Default.Encode(finalTitle)}\" />\n");
            output.Content.AppendHtml($"<meta property=\"og:description\" content=\"{HtmlEncoder.Default.Encode(finalDesc)}\" />\n");
            output.Content.AppendHtml($"<meta property=\"og:url\" content=\"{HtmlEncoder.Default.Encode(finalCanonical)}\" />\n");
            output.Content.AppendHtml($"<meta property=\"og:type\" content=\"{HtmlEncoder.Default.Encode(OgType)}\" />\n");
            output.Content.AppendHtml($"<meta property=\"og:image\" content=\"{HtmlEncoder.Default.Encode(finalOgImage)}\" />\n");

            // Twitter Cards
            output.Content.AppendHtml("<meta name=\"twitter:card\" content=\"summary_large_image\" />\n");
            output.Content.AppendHtml($"<meta name=\"twitter:title\" content=\"{HtmlEncoder.Default.Encode(finalTitle)}\" />\n");
            output.Content.AppendHtml($"<meta name=\"twitter:description\" content=\"{HtmlEncoder.Default.Encode(finalDesc)}\" />\n");
            output.Content.AppendHtml($"<meta name=\"twitter:image\" content=\"{HtmlEncoder.Default.Encode(finalOgImage)}\" />\n");

            // Structured Data JSON-LD
            if (!string.IsNullOrWhiteSpace(CustomJsonLd))
            {
                output.Content.AppendHtml($"<script type=\"application/ld+json\">\n{CustomJsonLd}\n</script>\n");
            }
            else
            {
                // Fallback structured data based on page type
                if (currentPath == "/" || currentPath.Equals("/public", StringComparison.OrdinalIgnoreCase))
                {
                    var orgSchema = new Dictionary<string, object?>
                    {
                        ["@context"] = "https://schema.org",
                        ["@type"] = "Organization",
                        ["name"] = siteName,
                        ["url"] = baseUrl,
                        ["logo"] = $"{baseUrl}/images/logo.png"
                    };
                    string json = JsonSerializer.Serialize(orgSchema, new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
                    output.Content.AppendHtml($"<script type=\"application/ld+json\">\n{json}\n</script>\n");
                }
            }
        }
    }
}

