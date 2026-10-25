namespace Catalog.Bll.DTOs;

using System.ComponentModel.DataAnnotations;

public record ServiceCategoryDto
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int ServicesCount { get; init; }
}

public record CreateCategoryDto
{
    [Required(ErrorMessage = "Назва категорії є обов'язковою.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Назва категорії повинна містити від 2 до 100 символів.")]
    public string Name { get; init; } = string.Empty;

    [StringLength(500, ErrorMessage = "Опис категорії не може перевищувати 500 символів.")]
    public string? Description { get; init; }
}

public record UpdateCategoryDto
{
    [Required(ErrorMessage = "Назва категорії є обов'язковою.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Назва категорії повинна містити від 2 до 100 символів.")]
    public string Name { get; init; } = string.Empty;

    [StringLength(500, ErrorMessage = "Опис категорії не може перевищувати 500 символів.")]
    public string? Description { get; init; }
}
