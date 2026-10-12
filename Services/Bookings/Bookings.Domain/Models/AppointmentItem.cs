namespace Bookings.Domain.Models;

/// <summary>
/// Позиція замовленої послуги (M:N зі знімком цін).
/// Складений PK: (AppointmentId, ServiceId).
/// </summary>
public class AppointmentItem
{
    public long AppointmentId { get; set; }
    public long ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public int Quantity { get; set; } = 1;
}
