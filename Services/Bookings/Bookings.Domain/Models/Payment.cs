namespace Bookings.Domain.Models;

/// <summary>
/// Платіж по замовленню (таблиця dbo.Payments).
/// </summary>
public class Payment
{
    public long Id { get; set; }
    public long AppointmentId { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = "Cash"; // Cash, Card, BankTransfer
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
}
