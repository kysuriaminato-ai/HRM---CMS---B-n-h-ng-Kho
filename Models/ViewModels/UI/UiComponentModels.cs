namespace ERP_HRM_CMS.Models.ViewModels.UI
{
    public record BreadcrumbItem(string Text, string? Url = null);

    public record WorkflowStatusBadgeInfo(string Label, string Variant, string Icon);

    public class PageHeaderModel
    {
        public string Title { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public string? Icon { get; set; }
        public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; set; } = Array.Empty<BreadcrumbItem>();
    }

    public class StatCardModel
    {
        public string Label { get; set; } = string.Empty;
        public string? Value { get; set; }
        public decimal? NumericTarget { get; set; }
        public string Prefix { get; set; } = string.Empty;
        public string Suffix { get; set; } = string.Empty;
        public int Decimals { get; set; }
        public string? Trend { get; set; }
        public string TrendDirection { get; set; } = "neutral"; // up | down | neutral
        public string Icon { get; set; } = "bi-bar-chart-line";
        public string Variant { get; set; } = "primary";
    }

    public class PaginationModel
    {
        public int PageIndex { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; }
        public int PageSize { get; set; } = 10;
        public string? BaseUrl { get; set; }
        public bool IsAjax { get; set; } = true;

        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;

        public static PaginationModel From<T>(PaginatedList<T> list, string? baseUrl = null, bool isAjax = false)
        {
            return new PaginationModel
            {
                PageIndex = list.PageIndex,
                TotalPages = Math.Max(1, list.TotalPages),
                TotalCount = list.TotalCount,
                PageSize = list.PageSize,
                BaseUrl = baseUrl,
                IsAjax = isAjax
            };
        }
    }

    public class EmptyStateModel
    {
        public string Icon { get; set; } = "bi-inbox";
        public string Title { get; set; } = "Chưa có dữ liệu";
        public string? Description { get; set; }
        public string? ActionText { get; set; }
        public string? ActionUrl { get; set; }
    }

    public class ConfirmModalModel
    {
        public string Id { get; set; } = "erpConfirmModal";
        public string Title { get; set; } = "Xác nhận thao tác";
        public string Message { get; set; } = "Bạn có chắc chắn muốn thực hiện hành động này?";
        public string ConfirmText { get; set; } = "Xác nhận";
        public string CancelText { get; set; } = "Hủy";
        public string Variant { get; set; } = "danger";
    }
}

