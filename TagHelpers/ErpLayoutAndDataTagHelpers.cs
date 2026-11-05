using System.Net;
using System.Text;
using Microsoft.AspNetCore.Razor.TagHelpers;
using ERP_HRM_CMS.Helpers;

namespace ERP_HRM_CMS.TagHelpers
{
    /// <summary>
    /// 1. PageHeader Tag Helper: &lt;erp-page-header title="..." subtitle="..." icon="..." breadcrumbs="Trang chủ:/|Nhân sự:/NhanSu"&gt;...actions...&lt;/erp-page-header&gt;
    /// </summary>
    [HtmlTargetElement("erp-page-header")]
    public class PageHeaderTagHelper : TagHelper
    {
        public string Title { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public string? Icon { get; set; }
        /// <summary>Chuỗi breadcrumb phân tách bởi '|', mỗi mục 'Nhãn:URL' hoặc 'Nhãn'.</summary>
        public string? Breadcrumbs { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var actionsContent = (await output.GetChildContentAsync()).GetContent();
            output.TagName = "div";
            output.TagMode = TagMode.StartTagAndEndTag;

            var existingClass = output.Attributes.ContainsName("class")
                ? " " + output.Attributes["class"].Value
                : string.Empty;
            output.Attributes.SetAttribute("class", "erp-page-header" + existingClass);

            var sb = new StringBuilder();
            sb.Append("<div class=\"erp-page-header-main\">");

            if (!string.IsNullOrWhiteSpace(Breadcrumbs))
            {
                sb.Append("<nav class=\"erp-page-breadcrumb\" aria-label=\"breadcrumb\"><ol class=\"breadcrumb\">");
                var parts = Breadcrumbs.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                for (int i = 0; i < parts.Length; i++)
                {
                    var seg = parts[i];
                    int colonIdx = seg.IndexOf(':');
                    bool isLast = i == parts.Length - 1;
                    if (!isLast && colonIdx > 0)
                    {
                        var label = WebUtility.HtmlEncode(seg[..colonIdx].Trim());
                        var rawUrl = seg[(colonIdx + 1)..].Trim();
                        var safeHref = UrlAnToan.LaHopLe(rawUrl) ? WebUtility.HtmlEncode(rawUrl) : "/";
                        sb.Append($"<li class=\"breadcrumb-item\"><a href=\"{safeHref}\">{label}</a></li>");
                    }
                    else
                    {
                        var label = WebUtility.HtmlEncode(colonIdx > 0 ? seg[..colonIdx].Trim() : seg);
                        sb.Append($"<li class=\"breadcrumb-item active\" aria-current=\"page\">{label}</li>");
                    }
                }
                sb.Append("</ol></nav>");
            }

            sb.Append("<div class=\"erp-page-header-title-row\">");
            if (!string.IsNullOrWhiteSpace(Icon))
            {
                sb.Append($"<span class=\"erp-page-header-icon\"><i class=\"bi {WebUtility.HtmlEncode(Icon)}\"></i></span>");
            }
            sb.Append($"<h1 class=\"erp-page-header-title\">{WebUtility.HtmlEncode(Title)}</h1>");
            sb.Append("</div>");

            if (!string.IsNullOrWhiteSpace(Subtitle))
            {
                sb.Append($"<p class=\"erp-page-header-subtitle\">{WebUtility.HtmlEncode(Subtitle)}</p>");
            }
            sb.Append("</div>");

            if (!string.IsNullOrWhiteSpace(actionsContent))
            {
                sb.Append("<div class=\"erp-page-header-actions\">");
                sb.Append(actionsContent);
                sb.Append("</div>");
            }

            output.Content.SetHtmlContent(sb.ToString());
        }
    }

    /// <summary>
    /// 2. StatCard Tag Helper: &lt;erp-stat-card label="..." value="..." count-up-target="1250" suffix=" đ" trend="+12%" trend-dir="up" icon="bi-people" variant="primary" /&gt;
    /// </summary>
    [HtmlTargetElement("erp-stat-card")]
    public class StatCardTagHelper : TagHelper
    {
        public string Label { get; set; } = string.Empty;
        public string? Value { get; set; }
        public decimal? CountUpTarget { get; set; }
        public string Prefix { get; set; } = string.Empty;
        public string Suffix { get; set; } = string.Empty;
        public int Decimals { get; set; }
        public string? Trend { get; set; }
        public string TrendDir { get; set; } = "neutral"; // up | down | neutral
        public string Icon { get; set; } = "bi-bar-chart-line";
        public string Variant { get; set; } = "primary";

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "div";
            output.TagMode = TagMode.StartTagAndEndTag;
            output.Attributes.SetAttribute("class", "stat-card hover-lift");

            string valueHtml;
            if (CountUpTarget.HasValue)
            {
                string invTarget = CountUpTarget.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                valueHtml = $"<span class=\"count-up\" data-target=\"{WebUtility.HtmlEncode(invTarget)}\" " +
                            $"data-prefix=\"{WebUtility.HtmlEncode(Prefix)}\" " +
                            $"data-suffix=\"{WebUtility.HtmlEncode(Suffix)}\" " +
                            $"data-decimals=\"{Decimals}\">{WebUtility.HtmlEncode(Prefix + CountUpTarget.Value.ToString($"N{Decimals}") + Suffix)}</span>";
            }
            else
            {
                valueHtml = WebUtility.HtmlEncode(Value ?? "0");
            }

            string trendHtml = string.Empty;
            if (!string.IsNullOrWhiteSpace(Trend))
            {
                string dir = TrendDir.ToLowerInvariant() switch
                {
                    "up" => "up",
                    "down" => "down",
                    _ => "neutral"
                };
                string trendIcon = dir switch
                {
                    "up" => "<i class=\"bi bi-arrow-up-right\"></i>",
                    "down" => "<i class=\"bi bi-arrow-down-right\"></i>",
                    _ => "<i class=\"bi bi-dash\"></i>"
                };
                trendHtml = $"<div class=\"stat-card-trend {dir}\">{trendIcon}<span>{WebUtility.HtmlEncode(Trend)}</span></div>";
            }

            output.Content.SetHtmlContent(
                $"<div class=\"stat-card-body\">" +
                    $"<div class=\"stat-card-label\">{WebUtility.HtmlEncode(Label)}</div>" +
                    $"<div class=\"stat-card-value\">{valueHtml}</div>" +
                    trendHtml +
                $"</div>" +
                $"<div class=\"stat-card-icon {WebUtility.HtmlEncode(Variant)}\">" +
                    $"<i class=\"bi {WebUtility.HtmlEncode(Icon)}\"></i>" +
                $"</div>");
        }
    }

    /// <summary>
    /// 3. FilterBar Tag Helper: &lt;erp-filter-bar target-table="..." action="..." search-name="keyword" search-value="..." search-placeholder="..." reset-url="..."&gt;...extra filters...&lt;/erp-filter-bar&gt;
    /// </summary>
    [HtmlTargetElement("erp-filter-bar")]
    public class FilterBarTagHelper : TagHelper
    {
        public string? TargetTable { get; set; }
        public string? ActionUrl { get; set; }
        public string SearchName { get; set; } = "keyword";
        public string? SearchValue { get; set; }
        public string SearchPlaceholder { get; set; } = "Tìm kiếm từ khóa...";
        public string? ResetUrl { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var extraFilters = (await output.GetChildContentAsync()).GetContent();
            output.TagName = "form";
            output.TagMode = TagMode.StartTagAndEndTag;
            output.Attributes.SetAttribute("method", "get");
            output.Attributes.SetAttribute("class", "erp-filter-bar");

            if (!string.IsNullOrWhiteSpace(ActionUrl) && UrlAnToan.LaHopLe(ActionUrl))
            {
                output.Attributes.SetAttribute("action", ActionUrl);
            }
            if (!string.IsNullOrWhiteSpace(TargetTable))
            {
                output.Attributes.SetAttribute("data-target-table", TargetTable);
            }

            var sb = new StringBuilder();
            sb.Append("<div class=\"erp-filter-search\">");
            sb.Append("<i class=\"bi bi-search\"></i>");
            sb.Append($"<input type=\"search\" name=\"{WebUtility.HtmlEncode(SearchName)}\" " +
                      $"value=\"{WebUtility.HtmlEncode(SearchValue ?? string.Empty)}\" " +
                      $"class=\"form-control\" placeholder=\"{WebUtility.HtmlEncode(SearchPlaceholder)}\" autocomplete=\"off\" />");
            sb.Append("</div>");

            if (!string.IsNullOrWhiteSpace(extraFilters))
            {
                sb.Append("<div class=\"erp-filter-controls\">");
                sb.Append(extraFilters);
                sb.Append("</div>");
            }

            sb.Append("<div class=\"erp-filter-actions\">");
            sb.Append("<button type=\"submit\" class=\"btn btn-primary\"><i class=\"bi bi-funnel me-1\"></i>Lọc</button>");
            if (!string.IsNullOrWhiteSpace(TargetTable))
            {
                sb.Append("<button type=\"button\" class=\"btn btn-outline-secondary\" data-filter-reset=\"true\"><i class=\"bi bi-arrow-counterclockwise me-1\"></i>Đặt lại</button>");
            }
            else if (!string.IsNullOrWhiteSpace(ResetUrl) && UrlAnToan.LaHopLe(ResetUrl))
            {
                sb.Append($"<a href=\"{WebUtility.HtmlEncode(ResetUrl)}\" class=\"btn btn-outline-secondary\"><i class=\"bi bi-arrow-counterclockwise me-1\"></i>Đặt lại</a>");
            }
            sb.Append("</div>");

            output.Content.SetHtmlContent(sb.ToString());
        }
    }

    /// <summary>
    /// 4. DataTable Tag Helper: hỗ trợ toolbar, sắp xếp cột, phân trang AJAX + loading overlay + stagger rows.
    /// </summary>
    [HtmlTargetElement("erp-data-table")]
    public class DataTableTagHelper : TagHelper
    {
        public string Id { get; set; } = "erpDataTable";
        public string? Title { get; set; }
        public string? AjaxUrl { get; set; }
        public int PageIndex { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; }
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; }
        public string SortDir { get; set; } = "asc";
        public bool Stagger { get; set; } = true;
        public bool ShowFooter { get; set; } = true;

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var innerTable = (await output.GetChildContentAsync()).GetContent();
            output.TagName = "div";
            output.TagMode = TagMode.StartTagAndEndTag;
            output.Attributes.SetAttribute("id", Id);

            string cls = "erp-table-wrapper" + (Stagger ? " stagger-rows" : string.Empty);
            output.Attributes.SetAttribute("class", cls);

            if (!string.IsNullOrWhiteSpace(AjaxUrl) && UrlAnToan.LaHopLe(AjaxUrl))
            {
                output.Attributes.SetAttribute("data-ajax-url", AjaxUrl);
            }
            output.Attributes.SetAttribute("data-page-index", PageIndex.ToString());
            output.Attributes.SetAttribute("data-page-size", PageSize.ToString());
            if (!string.IsNullOrWhiteSpace(SortBy))
            {
                output.Attributes.SetAttribute("data-sort-by", SortBy);
                output.Attributes.SetAttribute("data-sort-dir", SortDir);
            }

            var sb = new StringBuilder();
            sb.Append("<div class=\"erp-table-loading-overlay\" aria-hidden=\"true\"><span class=\"erp-spinner erp-spinner-lg\"></span></div>");

            if (!string.IsNullOrWhiteSpace(Title))
            {
                sb.Append("<div class=\"erp-table-toolbar\">");
                sb.Append($"<div class=\"erp-table-toolbar-title\">{WebUtility.HtmlEncode(Title)}</div>");
                if (TotalCount > 0)
                {
                    sb.Append($"<span class=\"erp-badge erp-badge-neutral\">{TotalCount:N0} bản ghi</span>");
                }
                sb.Append("</div>");
            }

            sb.Append("<div class=\"table-responsive\" data-erp-table-slot>");
            sb.Append(innerTable);
            sb.Append("</div>");

            if (ShowFooter && TotalPages >= 1 && TotalCount > 0)
            {
                int safeTotalPages = Math.Max(1, TotalPages);
                int from = ((PageIndex - 1) * PageSize) + 1;
                int to = Math.Min(PageIndex * PageSize, TotalCount);

                sb.Append("<div class=\"erp-table-footer\">");
                sb.Append("<div class=\"d-flex align-items-center gap-2\">");
                sb.Append($"<span>Hiển thị <strong>{from}–{to}</strong> / <strong>{TotalCount:N0}</strong></span>");
                if (!string.IsNullOrWhiteSpace(AjaxUrl))
                {
                    sb.Append("<select class=\"form-select form-select-sm w-auto py-1\" data-erp-page-size aria-label=\"Số dòng mỗi trang\">");
                    foreach (int opt in new[] { 5, 10, 20, 50 })
                    {
                        string sel = opt == PageSize ? " selected" : string.Empty;
                        sb.Append($"<option value=\"{opt}\"{sel}>{opt} / trang</option>");
                    }
                    sb.Append("</select>");
                }
                sb.Append("</div>");

                sb.Append("<nav aria-label=\"Phân trang bảng\"><ul class=\"erp-pagination\">");
                string prevDisabled = PageIndex <= 1 ? " disabled" : string.Empty;
                sb.Append($"<li><button type=\"button\" class=\"erp-page-btn{prevDisabled}\" data-erp-page=\"{PageIndex - 1}\"{prevDisabled} aria-label=\"Trang trước\"><i class=\"bi bi-chevron-left\"></i></button></li>");

                int startPage = Math.Max(1, PageIndex - 2);
                int endPage = Math.Min(safeTotalPages, startPage + 4);
                startPage = Math.Max(1, endPage - 4);

                for (int p = startPage; p <= endPage; p++)
                {
                    string active = p == PageIndex ? " active" : string.Empty;
                    sb.Append($"<li><button type=\"button\" class=\"erp-page-btn{active}\" data-erp-page=\"{p}\">{p}</button></li>");
                }

                string nextDisabled = PageIndex >= safeTotalPages ? " disabled" : string.Empty;
                sb.Append($"<li><button type=\"button\" class=\"erp-page-btn{nextDisabled}\" data-erp-page=\"{PageIndex + 1}\"{nextDisabled} aria-label=\"Trang sau\"><i class=\"bi bi-chevron-right\"></i></button></li>");
                sb.Append("</ul></nav>");
                sb.Append("</div>");
            }

            output.Content.SetHtmlContent(sb.ToString());
        }
    }

    /// <summary>
    /// 5. EmptyState Tag Helper: &lt;erp-empty-state icon="bi-inbox" title="..." description="..." action-text="..." action-url="..." /&gt;
    /// </summary>
    [HtmlTargetElement("erp-empty-state")]
    public class EmptyStateTagHelper : TagHelper
    {
        public string Icon { get; set; } = "bi-inbox";
        public string Title { get; set; } = "Chưa có dữ liệu";
        public string? Description { get; set; }
        public string? ActionText { get; set; }
        public string? ActionUrl { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var childHtml = (await output.GetChildContentAsync()).GetContent();
            output.TagName = "div";
            output.TagMode = TagMode.StartTagAndEndTag;
            output.Attributes.SetAttribute("class", "erp-empty-state");

            var sb = new StringBuilder();
            sb.Append($"<div class=\"erp-empty-state-icon\"><i class=\"bi {WebUtility.HtmlEncode(Icon)}\"></i></div>");
            sb.Append($"<div class=\"erp-empty-state-title\">{WebUtility.HtmlEncode(Title)}</div>");
            if (!string.IsNullOrWhiteSpace(Description))
            {
                sb.Append($"<div class=\"erp-empty-state-desc\">{WebUtility.HtmlEncode(Description)}</div>");
            }

            if (!string.IsNullOrWhiteSpace(ActionText) && !string.IsNullOrWhiteSpace(ActionUrl) && UrlAnToan.LaHopLe(ActionUrl))
            {
                sb.Append($"<div class=\"erp-empty-state-actions\"><a href=\"{WebUtility.HtmlEncode(ActionUrl)}\" class=\"btn btn-primary\">{WebUtility.HtmlEncode(ActionText)}</a></div>");
            }
            else if (!string.IsNullOrWhiteSpace(childHtml))
            {
                sb.Append($"<div class=\"erp-empty-state-actions\">{childHtml}</div>");
            }

            output.Content.SetHtmlContent(sb.ToString());
        }
    }

    /// <summary>
    /// 6. Skeleton Tag Helper: &lt;erp-skeleton type="card|table|list|avatar" rows="4" /&gt;
    /// </summary>
    [HtmlTargetElement("erp-skeleton")]
    public class SkeletonTagHelper : TagHelper
    {
        public string Type { get; set; } = "card"; // card | table | list | avatar
        public int Rows { get; set; } = 3;

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "div";
            output.TagMode = TagMode.StartTagAndEndTag;
            output.Attributes.SetAttribute("aria-busy", "true");
            output.Attributes.SetAttribute("aria-label", "Đang tải nội dung");

            int safeRows = Math.Clamp(Rows, 1, 10);
            var sb = new StringBuilder();

            switch (Type.ToLowerInvariant())
            {
                case "avatar":
                    output.Attributes.SetAttribute("class", "erp-skeleton-avatar-row");
                    sb.Append("<div class=\"skeleton-circle\"></div><div class=\"flex-grow-1\"><div class=\"skeleton-line w-50\"></div><div class=\"skeleton-line w-75\"></div></div>");
                    break;

                case "list":
                    output.Attributes.SetAttribute("class", "skeleton-card d-flex flex-column gap-3");
                    for (int i = 0; i < safeRows; i++)
                    {
                        sb.Append("<div class=\"erp-skeleton-avatar-row\"><div class=\"skeleton-circle\"></div><div class=\"flex-grow-1\"><div class=\"skeleton-line w-50\"></div><div class=\"skeleton-line w-75\"></div></div></div>");
                    }
                    break;

                case "table":
                    output.Attributes.SetAttribute("class", "erp-table-wrapper p-3");
                    for (int i = 0; i < safeRows; i++)
                    {
                        sb.Append("<div class=\"d-flex gap-3 mb-2\"><div class=\"skeleton-line w-25\"></div><div class=\"skeleton-line w-50\"></div><div class=\"skeleton-line w-25\"></div></div>");
                    }
                    break;

                default: // card
                    output.Attributes.SetAttribute("class", "skeleton-card");
                    sb.Append("<div class=\"skeleton-line w-50 mb-3\"></div>");
                    for (int i = 0; i < safeRows; i++)
                    {
                        string w = i == safeRows - 1 ? "w-75" : "w-100";
                        sb.Append($"<div class=\"skeleton-line {w}\"></div>");
                    }
                    break;
            }

            output.Content.SetHtmlContent(sb.ToString());
        }
    }
}

