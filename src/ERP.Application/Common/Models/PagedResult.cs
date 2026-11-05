namespace ERP.Application.Common.Models;

/// <summary>
/// Kết quả phân trang phía Server dùng chung trên toàn hệ thống.
/// </summary>
public sealed class PagedResult<T>
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 200;

    public PagedResult(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize)
    {
        PageNumber = pageNumber < 1 ? 1 : pageNumber;
        PageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
        TotalCount = totalCount < 0 ? 0 : totalCount;
        Items = items ?? Array.Empty<T>();
    }

    public IReadOnlyList<T> Items { get; }
    public int TotalCount { get; }
    public int PageNumber { get; }
    public int PageSize { get; }
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public static PagedResult<T> Empty(int pageNumber = 1, int pageSize = DefaultPageSize)
        => new(Array.Empty<T>(), 0, pageNumber, pageSize);

    public static PagedResult<T> Create(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize)
        => new(items, totalCount, pageNumber, pageSize);
}

