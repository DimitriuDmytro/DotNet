namespace Bookings.Domain.Models;

/// <summary>
/// Локальна read-model копія довідника послуг із Каталогу (таблиця dbo.Services).
/// Поле Id БЕЗ IDENTITY — отримується з Catalog сервісу.
/// </summary>
public class CatalogService
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationMin { get; set; }
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
}
