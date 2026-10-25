namespace Catalog.Bll.DTOs;

using System.ComponentModel.DataAnnotations;

public record ServiceDto
{
    public long Id { get; init; }
    public long CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int DurationMin { get; init; }
    public string? MainImageUrl { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record ServiceDetailDto
{
    public long Id { get; init; }
    public long CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int DurationMin { get; init; }
    public DateTime CreatedAt { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public DateTime? UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }

    public ServiceSpecDto? Spec { get; init; }
    public IReadOnlyList<ServiceImageDto> Images { get; init; } = Array.Empty<ServiceImageDto>();
    public IReadOnlyList<string> PackageNames { get; init; } = Array.Empty<string>();
}

public record CreateServiceDto
{
    [Range(1, long.MaxValue, ErrorMessage = "Ідентифікатор категорії (CategoryId) повинен бути додатним.")]
    public long CategoryId { get; init; }

    [Required(ErrorMessage = "Назва послуги обов'язкова.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Назва послуги повинна містити від 3 до 200 символів.")]
    public string Name { get; init; } = string.Empty;

    [Range(0.01, 10000000.00, ErrorMessage = "Вартість послуги повинна бути більшою за нуль.")]
    public decimal Price { get; init; }

    [Range(1, 2880, ErrorMessage = "Тривалість послуги повинна становити від 1 до 2880 хвилин.")]
    public int DurationMin { get; init; }

    public string? CreatedBy { get; init; }

    public UpsertServiceSpecDto? Spec { get; init; }
    public string? MainImageUrl { get; init; }
}

public record UpdateServiceDto
{
    [Range(1, long.MaxValue, ErrorMessage = "Ідентифікатор категорії (CategoryId) повинен бути додатним.")]
    public long CategoryId { get; init; }

    [Required(ErrorMessage = "Назва послуги обов'язкова.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Назва послуги повинна містити від 3 до 200 символів.")]
    public string Name { get; init; } = string.Empty;

    [Range(1, 2880, ErrorMessage = "Тривалість послуги повинна становити від 1 до 2880 хвилин.")]
    public int DurationMin { get; init; }

    public string? UpdatedBy { get; init; }
}

public record UpdatePriceDto
{
    [Range(0.01, 10000000.00, ErrorMessage = "Нова вартість послуги повинна бути більшою за нуль.")]
    public decimal NewPrice { get; init; }

    public string? UpdatedBy { get; init; }
}
