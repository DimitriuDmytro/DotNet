namespace Bookings.Bll.DTOs;

using System.ComponentModel.DataAnnotations;

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
    [Range(1, long.MaxValue, ErrorMessage = "Ідентифікатор послуги повинен бути додатним числом.")]
    public long Id { get; set; }

    [Required(ErrorMessage = "Назва послуги обов'язкова.")]
    [StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, 10000000.00, ErrorMessage = "Ціна повинна бути більшою за нуль.")]
    public decimal Price { get; set; }

    [Range(1, 10080, ErrorMessage = "Тривалість послуги у хвилинах повинна бути додатною.")]
    public int DurationMin { get; set; }
}
