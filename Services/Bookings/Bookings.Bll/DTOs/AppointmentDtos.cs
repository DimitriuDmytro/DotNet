namespace Bookings.Bll.DTOs;

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
    public long ServiceId { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal Discount { get; set; } = 0.00m;
}

public class CreateAppointmentDto
{
    public long ClientId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public string CarBrand { get; set; } = string.Empty;
    public string CarModel { get; set; } = string.Empty;
    public string PlateNumber { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public string? CreatedBy { get; set; }
    public List<CreateAppointmentItemDto> Items { get; set; } = new();
}

public class ChangeStatusDto
{
    public string NewStatus { get; set; } = string.Empty;
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
