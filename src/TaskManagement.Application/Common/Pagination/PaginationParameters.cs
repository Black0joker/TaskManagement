namespace TaskManagement.Application.Common.Pagination;

/// <summary>
/// Normalized pagination input. Page is always >= 1 and PageSize is
/// clamped to [1, MaxPageSize] so clients can never request unlimited rows.
/// </summary>
public sealed record PaginationParameters(int Page = 1, int PageSize = 20)
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public int NormalizedPage => Page < 1 ? 1 : Page;
    public int NormalizedPageSize => PageSize < 1 ? DefaultPageSize : Math.Min(PageSize, MaxPageSize);
}
