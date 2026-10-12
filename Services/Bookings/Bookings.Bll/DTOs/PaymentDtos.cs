namespace Bookings.Bll.DTOs;

public class PaymentDto
{
    public long Id { get; set; }
    public long AppointmentId { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public DateTime PaidAt { get; set; }
}

public class RegisterPaymentDto
{
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
}
