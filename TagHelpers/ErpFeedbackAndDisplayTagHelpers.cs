using System.Net;
using System.Text;
using Microsoft.AspNetCore.Razor.TagHelpers;
using ERP_HRM_CMS.Helpers;

namespace ERP_HRM_CMS.TagHelpers
{
    /// <summary>
    /// 7. ConfirmModal Tag Helper: &lt;erp-confirm-modal id="..." title="..." message="..." confirm-text="..." variant="danger" /&gt;
    /// </summary>
    [HtmlTargetElement("erp-confirm-modal")]
    public class ConfirmModalTagHelper : TagHelper
    {
        public string Id { get; set; } = "erpConfirmModal";
        public string Title { get; set; } = "Xác nhận thao tác";
        public string Message { get; set; } = "Bạn có chắc chắn muốn thực hiện hành động này?";
        public string ConfirmText { get; set; } = "Xác nhận";
        public string CancelText { get; set; } = "Hủy";
        public string Variant { get; set; } = "danger";

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            string icon = Variant.ToLowerInvariant() == "warning" ? "bi-exclamation-triangle-fill" : "bi-trash3-fill";

            output.TagName = "div";
            output.TagMode = TagMode.StartTagAndEndTag;
            output.Attributes.SetAttribute("class", "modal fade erp-confirm-modal");
            output.Attributes.SetAttribute("id", Id);
            output.Attributes.SetAttribute("tabindex", "-1");
            output.Attributes.SetAttribute("aria-hidden", "true");

            output.Content.SetHtmlContent(
                $"<div class=\"modal-dialog modal-dialog-centered modal-sm\">" +
                    $"<div class=\"modal-content\">" +
                        $"<div class=\"modal-body\">" +
                            $"<div class=\"erp-confirm-icon {WebUtility.HtmlEncode(Variant)}\"><i class=\"bi {icon}\"></i></div>" +
                            $"<h6 class=\"fw-bold mb-2\">{WebUtility.HtmlEncode(Title)}</h6>" +
                            $"<p class=\"text-muted small mb-0\">{WebUtility.HtmlEncode(Message)}</p>" +
                        $"</div>" +
                        $"<div class=\"modal-footer border-0 pt-0 pb-4 justify-content-center gap-2\">" +
                            $"<button type=\"button\" class=\"btn btn-sm btn-outline-secondary px-3\" data-bs-dismiss=\"modal\">{WebUtility.HtmlEncode(CancelText)}</button>" +
                            $"<button type=\"button\" class=\"btn btn-sm btn-{WebUtility.HtmlEncode(Variant)} px-3\" data-confirm-action>{WebUtility.HtmlEncode(ConfirmText)}</button>" +
                        $"</div>" +
                    $"</div>" +
                $"</div>");
        }
    }

    internal sealed class DrawerContext
    {
        public string? FooterHtml { get; set; }
    }

    [HtmlTargetElement("erp-drawer-footer", ParentTag = "erp-drawer")]
    public class DrawerFooterTagHelper : TagHelper
    {
        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            if (context.Items.TryGetValue(typeof(DrawerContext), out var obj) && obj is DrawerContext dc)
            {
                dc.FooterHtml = (await output.GetChildContentAsync()).GetContent();
            }
            output.SuppressOutput();
        }
    }

    /// <summary>
    /// 8. Drawer Tag Helper: &lt;erp-drawer id="..." title="..." subtitle="..." size="md" position="end"&gt;...body...&lt;erp-drawer-footer&gt;...&lt;/erp-drawer-footer&gt;&lt;/erp-drawer&gt;
    /// </summary>
    [HtmlTargetElement("erp-drawer")]
    public class DrawerTagHelper : TagHelper
    {
        public string Id { get; set; } = "erpDrawer";
        public string Title { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public string Size { get; set; } = "md";       // sm | md | lg
        public string Position { get; set; } = "end";  // end | start

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var drawerCtx = new DrawerContext();
            context.Items[typeof(DrawerContext)] = drawerCtx;

            var bodyHtml = (await output.GetChildContentAsync()).GetContent();
            string posClass = Position.Equals("start", StringComparison.OrdinalIgnoreCase) ? "erp-drawer-start" : "erp-drawer-end";
            string sizeClass = Size.ToLowerInvariant() switch
            {
                "sm" => "erp-drawer-sm",
                "lg" => "erp-drawer-lg",
                _ => "erp-drawer-md"
            };

            output.TagName = null; // Render cả backdrop + aside
            var sb = new StringBuilder();
            sb.Append($"<div class=\"erp-drawer-backdrop\" data-drawer-backdrop=\"{WebUtility.HtmlEncode(Id)}\"></div>");
            sb.Append($"<aside class=\"erp-drawer {posClass} {sizeClass}\" id=\"{WebUtility.HtmlEncode(Id)}\" role=\"dialog\" aria-modal=\"true\" aria-hidden=\"true\" aria-labelledby=\"{WebUtility.HtmlEncode(Id)}_title\">");
            sb.Append("<div class=\"erp-drawer-header\"><div>");
            sb.Append($"<h5 class=\"erp-drawer-title\" id=\"{WebUtility.HtmlEncode(Id)}_title\">{WebUtility.HtmlEncode(Title)}</h5>");
            if (!string.IsNullOrWhiteSpace(Subtitle))
            {
                sb.Append($"<p class=\"erp-drawer-subtitle\">{WebUtility.HtmlEncode(Subtitle)}</p>");
            }
            sb.Append("</div>");
            sb.Append($"<button type=\"button\" class=\"btn-close\" data-drawer-close=\"{WebUtility.HtmlEncode(Id)}\" aria-label=\"Đóng\"></button>");
            sb.Append("</div>");

            sb.Append($"<div class=\"erp-drawer-body\">{bodyHtml}</div>");
            if (!string.IsNullOrWhiteSpace(drawerCtx.FooterHtml))
            {
                sb.Append($"<div class=\"erp-drawer-footer\">{drawerCtx.FooterHtml}</div>");
            }
            sb.Append("</aside>");

            output.Content.SetHtmlContent(sb.ToString());
        }
    }

    internal sealed class TabsContext
    {
        public List<(string Id, string Title, string? Icon, string? Badge, bool Active, string Content)> Items { get; } = new();
    }

    [HtmlTargetElement("erp-tab", ParentTag = "erp-tabs")]
    public class TabItemTagHelper : TagHelper
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public string? Badge { get; set; }
        public bool Active { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var body = (await output.GetChildContentAsync()).GetContent();
            if (context.Items.TryGetValue(typeof(TabsContext), out var obj) && obj is TabsContext tc)
            {
                string tabId = string.IsNullOrWhiteSpace(Id) ? $"erp_tab_{tc.Items.Count + 1}_{Guid.NewGuid():N}"[..18] : Id;
                tc.Items.Add((tabId, Title, Icon, Badge, Active, body));
            }
            output.SuppressOutput();
        }
    }

    /// <summary>
    /// 9. Tabs Tag Helper (có sliding indicator): &lt;erp-tabs id="..."&gt;&lt;erp-tab id="..." title="..." active="true"&gt;...&lt;/erp-tab&gt;&lt;/erp-tabs&gt;
    /// </summary>
    [HtmlTargetElement("erp-tabs")]
    public class TabsTagHelper : TagHelper
    {
        public string? Id { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var tabsCtx = new TabsContext();
            context.Items[typeof(TabsContext)] = tabsCtx;
            await output.GetChildContentAsync();

            output.TagName = "div";
            output.TagMode = TagMode.StartTagAndEndTag;
            output.Attributes.SetAttribute("class", "erp-tabs-container");
            if (!string.IsNullOrWhiteSpace(Id))
            {
                output.Attributes.SetAttribute("id", Id);
            }

            if (tabsCtx.Items.Count > 0 && !tabsCtx.Items.Any(i => i.Active))
            {
                var first = tabsCtx.Items[0];
                tabsCtx.Items[0] = (first.Id, first.Title, first.Icon, first.Badge, true, first.Content);
            }

            var sb = new StringBuilder();
            sb.Append("<div class=\"erp-tabs\" role=\"tablist\">");
            foreach (var item in tabsCtx.Items)
            {
                string activeCls = item.Active ? " active" : string.Empty;
                string selected = item.Active ? "true" : "false";
                string tabIdx = item.Active ? "0" : "-1";
                sb.Append($"<button type=\"button\" class=\"erp-tab{activeCls}\" role=\"tab\" " +
                          $"aria-selected=\"{selected}\" aria-controls=\"{WebUtility.HtmlEncode(item.Id)}\" " +
                          $"data-tab-target=\"{WebUtility.HtmlEncode(item.Id)}\" tabindex=\"{tabIdx}\">");
                if (!string.IsNullOrWhiteSpace(item.Icon))
                {
                    sb.Append($"<i class=\"bi {WebUtility.HtmlEncode(item.Icon)}\"></i>");
                }
                sb.Append($"<span>{WebUtility.HtmlEncode(item.Title)}</span>");
                if (!string.IsNullOrWhiteSpace(item.Badge))
                {
                    sb.Append($"<span class=\"erp-tab-badge\">{WebUtility.HtmlEncode(item.Badge)}</span>");
                }
                sb.Append("</button>");
            }
            sb.Append("<span class=\"erp-tab-indicator\" aria-hidden=\"true\"></span>");
            sb.Append("</div>");

            sb.Append("<div class=\"erp-tab-panels\">");
            foreach (var item in tabsCtx.Items)
            {
                string activeCls = item.Active ? " active" : string.Empty;
                string hiddenAttr = item.Active ? string.Empty : " hidden";
                sb.Append($"<div class=\"erp-tab-panel{activeCls}\" id=\"{WebUtility.HtmlEncode(item.Id)}\" role=\"tabpanel\"{hiddenAttr}>");
                sb.Append(item.Content);
                sb.Append("</div>");
            }
            sb.Append("</div>");

            output.Content.SetHtmlContent(sb.ToString());
        }
    }

    /// <summary>
    /// 10. WorkflowStatuses Badge Tag Helper: &lt;erp-status-badge status="@item.Status" workflow="LeaveRequest" dot="true" /&gt;
    /// </summary>
    [HtmlTargetElement("erp-status-badge")]
    public class StatusBadgeTagHelper : TagHelper
    {
        public object? Status { get; set; }
        public string? Workflow { get; set; }
        public bool Dot { get; set; } = true;
        public bool ShowIcon { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var info = WorkflowStatusUiHelper.ResolveObject(Status, Workflow);
            output.TagName = "span";
            output.TagMode = TagMode.StartTagAndEndTag;

            string dotClass = Dot && !ShowIcon ? " erp-badge-dot" : string.Empty;
            output.Attributes.SetAttribute("class", $"erp-badge erp-badge-{WebUtility.HtmlEncode(info.Variant)}{dotClass}");

            string iconHtml = ShowIcon
                ? $"<i class=\"bi {WebUtility.HtmlEncode(info.Icon)}\"></i>"
                : string.Empty;
            output.Content.SetHtmlContent($"{iconHtml}<span>{WebUtility.HtmlEncode(info.Label)}</span>");
        }
    }

    /// <summary>
    /// 11. Avatar Tag Helper: &lt;erp-avatar name="Nguyễn Văn A" src="/uploads/..." size="md" status="online" /&gt;
    /// </summary>
    [HtmlTargetElement("erp-avatar")]
    public class AvatarTagHelper : TagHelper
    {
        public string Name { get; set; } = "U";
        public string? Src { get; set; }
        public string Size { get; set; } = "md"; // xs | sm | md | lg | xl
        public string? Status { get; set; }      // online | offline | busy | away

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            string initials = ExtractInitials(Name);
            int tone = Math.Abs((Name ?? string.Empty).GetHashCode()) % 6;
            string sizeCls = Size.ToLowerInvariant() switch
            {
                "xs" => "erp-avatar-xs",
                "sm" => "erp-avatar-sm",
                "lg" => "erp-avatar-lg",
                "xl" => "erp-avatar-xl",
                _ => "erp-avatar-md"
            };

            output.TagName = "span";
            output.TagMode = TagMode.StartTagAndEndTag;
            output.Attributes.SetAttribute("class", $"erp-avatar {sizeCls} erp-avatar-tone-{tone}");
            output.Attributes.SetAttribute("title", Name);

            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(Src) && UrlAnToan.LaHopLe(Src))
            {
                sb.Append($"<img src=\"{WebUtility.HtmlEncode(Src)}\" alt=\"{WebUtility.HtmlEncode(Name)}\" class=\"erp-avatar-img\" " +
                          $"onerror=\"this.remove();\" />");
            }
            else
            {
                sb.Append($"<span class=\"erp-avatar-initials\">{WebUtility.HtmlEncode(initials)}</span>");
            }

            if (!string.IsNullOrWhiteSpace(Status))
            {
                string st = Status.ToLowerInvariant();
                if (st is "online" or "offline" or "busy" or "away")
                {
                    sb.Append($"<span class=\"erp-avatar-status {st}\" aria-label=\"{st}\"></span>");
                }
            }

            output.Content.SetHtmlContent(sb.ToString());
        }

        private static string ExtractInitials(string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "U";
            var words = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 1) return words[0][0].ToString().ToUpperInvariant();
            return string.Concat(char.ToUpperInvariant(words[0][0]), char.ToUpperInvariant(words[^1][0]));
        }
    }

    /// <summary>
    /// 12. Timeline Tag Helpers: &lt;erp-timeline&gt;&lt;erp-timeline-item title="..." time="..." actor="..." variant="primary" icon="bi-check"&gt;...&lt;/erp-timeline-item&gt;&lt;/erp-timeline&gt;
    /// </summary>
    [HtmlTargetElement("erp-timeline")]
    public class TimelineTagHelper : TagHelper
    {
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "ul";
            output.TagMode = TagMode.StartTagAndEndTag;
            output.Attributes.SetAttribute("class", "erp-timeline");
        }
    }

    [HtmlTargetElement("erp-timeline-item", ParentTag = "erp-timeline")]
    public class TimelineItemTagHelper : TagHelper
    {
        public string Title { get; set; } = string.Empty;
        public string? Time { get; set; }
        public string? Actor { get; set; }
        public string Variant { get; set; } = "primary";
        public string Icon { get; set; } = "bi-circle-fill";

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var body = (await output.GetChildContentAsync()).GetContent();
            output.TagName = "li";
            output.TagMode = TagMode.StartTagAndEndTag;
            output.Attributes.SetAttribute("class", "erp-timeline-item");

            var sb = new StringBuilder();
            sb.Append($"<span class=\"erp-timeline-marker {WebUtility.HtmlEncode(Variant)}\"><i class=\"bi {WebUtility.HtmlEncode(Icon)}\"></i></span>");
            sb.Append("<div class=\"erp-timeline-content\">");
            sb.Append("<div class=\"erp-timeline-header\">");
            sb.Append($"<span class=\"erp-timeline-title\">{WebUtility.HtmlEncode(Title)}</span>");
            if (!string.IsNullOrWhiteSpace(Time))
            {
                sb.Append($"<time class=\"erp-timeline-time\">{WebUtility.HtmlEncode(Time)}</time>");
            }
            sb.Append("</div>");
            if (!string.IsNullOrWhiteSpace(Actor))
            {
                sb.Append($"<div class=\"erp-timeline-actor\"><i class=\"bi bi-person me-1\"></i>{WebUtility.HtmlEncode(Actor)}</div>");
            }
            if (!string.IsNullOrWhiteSpace(body))
            {
                sb.Append($"<div class=\"erp-timeline-body\">{body}</div>");
            }
            sb.Append("</div>");

            output.Content.SetHtmlContent(sb.ToString());
        }
    }
}

