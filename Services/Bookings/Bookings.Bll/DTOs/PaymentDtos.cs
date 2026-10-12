namespace Bookings.Bll.DTOs;

using System.ComponentModel.DataAnnotations;

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
    [Range(0.01, 10000000.00, ErrorMessage = "Сума платежу повинна бути більшою за нуль.")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Метод оплати обов'язковий (Cash, Card, BankTransfer).")]
    [StringLength(30, MinimumLength = 3)]
    public string Method { get; set; } = string.Empty;
}
