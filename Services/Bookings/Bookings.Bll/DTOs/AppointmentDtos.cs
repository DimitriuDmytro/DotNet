namespace Bookings.Bll.DTOs;

using System.ComponentModel.DataAnnotations;

public record AppointmentDto
{
    public long Id { get; init; }
    public long ClientId { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime ScheduledAt { get; init; }
    public decimal TotalAmount { get; init; }
    public DateTime CreatedAt { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public DateTime? UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }

    public AppointmentDetailsDto? Details { get; init; }
    public List<AppointmentItemDto> Items { get; init; } = new();
}

public record AppointmentDetailsDto
{
    public long AppointmentId { get; init; }
    public string CarBrand { get; init; } = string.Empty;
    public string CarModel { get; init; } = string.Empty;
    public string PlateNumber { get; init; } = string.Empty;
    public string? Comment { get; init; }
}

public record AppointmentItemDto
{
    public long AppointmentId { get; init; }
    public long ServiceId { get; init; }
    public string ServiceName { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public decimal Discount { get; init; }
    public int Quantity { get; init; }
}

public record CreateAppointmentItemDto
{
    [Range(1, long.MaxValue, ErrorMessage = "Ідентифікатор послуги повинен бути додатним.")]
    public long ServiceId { get; init; }

    [Range(1, 100, ErrorMessage = "Кількість одиниць послуги повинна бути від 1 до 100.")]
    public int Quantity { get; init; } = 1;

    [Range(0.0, 1000000.0, ErrorMessage = "Знижка не може бути від'ємною.")]
    public decimal Discount { get; init; } = 0.00m;
}

public record CreateAppointmentDto
{
    [Range(1, long.MaxValue, ErrorMessage = "Ідентифікатор клієнта повинен бути додатним числом.")]
    public long ClientId { get; init; }

    [Required(ErrorMessage = "Дата та час візиту обов'язкові.")]
    public DateTime ScheduledAt { get; init; }

    [Required(ErrorMessage = "Марка автомобіля обов'язкова.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Марка автомобіля має містити від 1 до 100 символів.")]
    public string CarBrand { get; init; } = string.Empty;

    [Required(ErrorMessage = "Модель автомобіля обов'язкова.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Модель автомобіля має містити від 1 до 100 символів.")]
    public string CarModel { get; init; } = string.Empty;

    [Required(ErrorMessage = "Державний номер автомобіля обов'язковий.")]
    [StringLength(20, MinimumLength = 2, ErrorMessage = "Державний номер має містити від 2 до 20 символів.")]
    public string PlateNumber { get; init; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Коментар не може перевищувати 1000 символів.")]
    public string? Comment { get; init; }

    [StringLength(100)]
    public string? CreatedBy { get; init; }

    [Required(ErrorMessage = "Список послуг обов'язковий.")]
    [MinLength(1, ErrorMessage = "Запис повинен містити щонайменше одну послугу.")]
    public List<CreateAppointmentItemDto> Items { get; init; } = new();
}

public record ChangeStatusDto
{
    [Required(ErrorMessage = "Новий статус обов'язковий.")]
    [StringLength(30, MinimumLength = 3)]
    public string NewStatus { get; init; } = string.Empty;

    [StringLength(100)]
    public string? ChangedBy { get; init; }
}

public record StatusHistoryDto
{
    public long Id { get; init; }
    public long AppointmentId { get; init; }
    public string? OldStatus { get; init; }
    public string NewStatus { get; init; } = string.Empty;
    public DateTime ChangedAt { get; init; }
    public string ChangedBy { get; init; } = string.Empty;
}
