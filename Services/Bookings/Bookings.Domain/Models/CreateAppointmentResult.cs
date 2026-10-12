namespace Bookings.Domain.Models;

/// <summary>
/// Результат створення нового запису на обслуговування (замість анонімного кортежу).
/// </summary>
public record CreateAppointmentResult(
    long AppointmentId,
    decimal TotalAmount,
    string Status
);
