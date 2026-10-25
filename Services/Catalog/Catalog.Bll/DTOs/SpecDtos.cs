namespace Catalog.Bll.DTOs;

using System.ComponentModel.DataAnnotations;

public record ServiceSpecDto
{
    public long ServiceId { get; init; }
    public string Materials { get; init; } = string.Empty;
    public int WarrantyMonths { get; init; }
    public string Description { get; init; } = string.Empty;
}

public record UpsertServiceSpecDto
{
    [Required(ErrorMessage = "Перелік матеріалів обов'язковий.")]
    [StringLength(500, MinimumLength = 3, ErrorMessage = "Перелік матеріалів повинен містити від 3 до 500 символів.")]
    public string Materials { get; init; } = string.Empty;

    [Range(0, 120, ErrorMessage = "Гарантійний термін повинен бути від 0 до 120 місяців.")]
    public int WarrantyMonths { get; init; }

    [Required(ErrorMessage = "Технологічний опис послуги обов'язковий.")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Технологічний опис повинен містити від 10 до 2000 символів.")]
    public string Description { get; init; } = string.Empty;
}
