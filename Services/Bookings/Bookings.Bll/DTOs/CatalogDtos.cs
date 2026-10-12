namespace Bookings.Bll.DTOs;

public class CatalogServiceDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationMin { get; set; }
    public DateTime SyncedAt { get; set; }
}

public class UpsertCatalogServiceDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationMin { get; set; }
}
