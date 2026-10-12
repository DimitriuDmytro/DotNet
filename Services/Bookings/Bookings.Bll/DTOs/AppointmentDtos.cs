namespace Bookings.Bll.DTOs;

using System.ComponentModel.DataAnnotations;

public class AppointmentDto
{
    public long Id { get; set; }
    public long ClientId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    public AppointmentDetailsDto? Details { get; set; }
    public List<AppointmentItemDto> Items { get; set; } = new();
}

public class AppointmentDetailsDto
{
    public long AppointmentId { get; set; }
    public string CarBrand { get; set; } = string.Empty;
    public string CarModel { get; set; } = string.Empty;
    public string PlateNumber { get; set; } = string.Empty;
    public string? Comment { get; set; }
}

public class AppointmentItemDto
{
    public long AppointmentId { get; set; }
    public long ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public int Quantity { get; set; }
}

public class CreateAppointmentItemDto
{
    [Range(1, long.MaxValue, ErrorMessage = "Ідентифікатор послуги повинен бути додатним.")]
    public long ServiceId { get; set; }

    [Range(1, 100, ErrorMessage = "Кількість одиниць послуги повинна бути від 1 до 100.")]
    public int Quantity { get; set; } = 1;

    [Range(0.0, 1000000.0, ErrorMessage = "Знижка не може бути від'ємною.")]
    public decimal Discount { get; set; } = 0.00m;
}

public class CreateAppointmentDto
{
    [Range(1, long.MaxValue, ErrorMessage = "Ідентифікатор клієнта повинен бути додатним числом.")]
    public long ClientId { get; set; }

    [Required(ErrorMessage = "Дата та час візиту обов'язкові.")]
    public DateTime ScheduledAt { get; set; }

    [Required(ErrorMessage = "Марка автомобіля обов'язкова.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Марка автомобіля має містити від 1 до 100 символів.")]
    public string CarBrand { get; set; } = string.Empty;

    [Required(ErrorMessage = "Модель автомобіля обов'язкова.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Модель автомобіля має містити від 1 до 100 символів.")]
    public string CarModel { get; set; } = string.Empty;

    [Required(ErrorMessage = "Державний номер автомобіля обов'язковий.")]
    [StringLength(20, MinimumLength = 2, ErrorMessage = "Державний номер має містити від 2 до 20 символів.")]
    public string PlateNumber { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Коментар не може перевищувати 1000 символів.")]
    public string? Comment { get; set; }

    [StringLength(100)]
    public string? CreatedBy { get; set; }

    [Required(ErrorMessage = "Список послуг обов'язковий.")]
    [MinLength(1, ErrorMessage = "Запис повинен містити щонайменше одну послугу.")]
    public List<CreateAppointmentItemDto> Items { get; set; } = new();
}

public class ChangeStatusDto
{
    [Required(ErrorMessage = "Новий статус обов'язковий.")]
    [StringLength(30, MinimumLength = 3)]
    public string NewStatus { get; set; } = string.Empty;

    [StringLength(100)]
    public string? ChangedBy { get; set; }
}

public class StatusHistoryDto
{
    public long Id { get; set; }
    public long AppointmentId { get; set; }
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
}
