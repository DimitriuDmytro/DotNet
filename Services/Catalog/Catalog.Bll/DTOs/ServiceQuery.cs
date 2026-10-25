namespace Catalog.Bll.DTOs;

/// <summary>
/// Параметри фільтрації, пошуку, сортування та пагінації каталогу послуг (Критерій 10).
/// </summary>
public record ServiceQuery
{
    private int _page = 1;
    private int _pageSize = 20;

    public int Page
    {
        get => _page;
        init => _page = Math.Max(1, value);
    }

    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = Math.Clamp(value, 1, 100);
    }

    public string? Search { get; init; }
    public long? CategoryId { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public string? SortBy { get; init; } = "name";
    public string? SortDir { get; init; } = "asc";
}
