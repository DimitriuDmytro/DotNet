namespace Bookings.Bll.DTOs;

/// <summary>
/// Універсальна обгортка для пагінованого списку результатів.
/// </summary>
/// <typeparam name="T">Тип елемента списку</typeparam>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}
