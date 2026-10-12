namespace Bookings.Bll.DTOs;

/// <summary>
/// Універсальна обгортка для пагінованого списку результатів.
/// </summary>
/// <typeparam name="T">Тип елемента списку</typeparam>
public record PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
}
