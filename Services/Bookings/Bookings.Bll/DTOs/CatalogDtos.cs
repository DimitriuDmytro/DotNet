namespace Bookings.Bll.DTOs;

using System.ComponentModel.DataAnnotations;

public record CatalogServiceDto
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int DurationMin { get; init; }
    public DateTime SyncedAt { get; init; }
}

public record UpsertCatalogServiceDto
{
    [Range(1, long.MaxValue, ErrorMessage = "Ідентифікатор послуги повинен бути додатним числом.")]
    public long Id { get; init; }

    [Required(ErrorMessage = "Назва послуги обов'язкова.")]
    [StringLength(200, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [Range(0.01, 10000000.00, ErrorMessage = "Ціна повинна бути більшою за нуль.")]
    public decimal Price { get; init; }

    [Range(1, 10080, ErrorMessage = "Тривалість послуги у хвилинах повинна бути додатною.")]
    public int DurationMin { get; init; }
}
