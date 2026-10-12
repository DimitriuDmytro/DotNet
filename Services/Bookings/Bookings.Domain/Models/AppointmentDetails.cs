namespace Bookings.Domain.Models;

/// <summary>
/// Деталі автомобіля в межах запису (Зв'язок 1:1 через спільний PK).
/// </summary>
public class AppointmentDetails
{
    public long AppointmentId { get; set; }
    public string CarBrand { get; set; } = string.Empty;
    public string CarModel { get; set; } = string.Empty;
    public string PlateNumber { get; set; } = string.Empty;
    public string? Comment { get; set; }
}
