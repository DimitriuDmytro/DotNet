namespace Bookings.Bll.DTOs;

using System.ComponentModel.DataAnnotations;

public record PaymentDto
{
    public long Id { get; init; }
    public long AppointmentId { get; init; }
    public decimal Amount { get; init; }
    public string Method { get; init; } = string.Empty;
    public DateTime PaidAt { get; init; }
}

public record RegisterPaymentDto
{
    [Range(0.01, 10000000.00, ErrorMessage = "Сума платежу повинна бути більшою за нуль.")]
    public decimal Amount { get; init; }

    [Required(ErrorMessage = "Метод оплати обов'язковий (Cash, Card, BankTransfer).")]
    [StringLength(30, MinimumLength = 3)]
    public string Method { get; init; } = string.Empty;
}
