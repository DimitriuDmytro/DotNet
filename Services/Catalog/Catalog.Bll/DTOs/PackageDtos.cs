namespace Catalog.Bll.DTOs;

using System.ComponentModel.DataAnnotations;

public record PackageItemDto
{
    public long ServiceId { get; init; }
    public string ServiceName { get; init; } = string.Empty;
    public decimal ServicePrice { get; init; }
    public int SortOrder { get; init; }
    public DateTime AddedAt { get; init; }
}

public record PackageDto
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public int ServicesCount { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record PackageDetailDto
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public DateTime CreatedAt { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public DateTime? UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }

    public IReadOnlyList<PackageItemDto> Services { get; init; } = Array.Empty<PackageItemDto>();
}

public record CreatePackageDto
{
    [Required(ErrorMessage = "Назва пакета обов'язкова.")]
    [StringLength(150, MinimumLength = 3, ErrorMessage = "Назва пакета повинна містити від 3 до 150 символів.")]
    public string Name { get; init; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Опис пакета не може перевищувати 1000 символів.")]
    public string? Description { get; init; }

    [Range(0.01, 10000000.00, ErrorMessage = "Вартість пакета повинна бути більшою за нуль.")]
    public decimal Price { get; init; }

    public string? CreatedBy { get; init; }

    public List<AddServiceToPackageDto> Services { get; init; } = new();
}

public record UpdatePackageDto
{
    [Required(ErrorMessage = "Назва пакета обов'язкова.")]
    [StringLength(150, MinimumLength = 3, ErrorMessage = "Назва пакета повинна містити від 3 до 150 символів.")]
    public string Name { get; init; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Опис пакета не може перевищувати 1000 символів.")]
    public string? Description { get; init; }

    [Range(0.01, 10000000.00, ErrorMessage = "Вартість пакета повинна бути більшою за нуль.")]
    public decimal Price { get; init; }

    public string? UpdatedBy { get; init; }
}

public record AddServiceToPackageDto
{
    [Range(1, long.MaxValue, ErrorMessage = "Ідентифікатор послуги повинен бути додатним.")]
    public long ServiceId { get; init; }

    [Range(1, 100, ErrorMessage = "Порядковий номер послуги у пакеті повинен бути від 1 до 100.")]
    public int SortOrder { get; init; } = 1;
}
