namespace Bookings.Bll.Interfaces;

using Bookings.Bll.DTOs;

public interface IPaymentService
{
    Task<IReadOnlyList<PaymentDto>> GetByAppointmentIdAsync(long appointmentId, CancellationToken cancellationToken = default);
    Task<PaymentDto> RegisterPaymentAsync(long appointmentId, RegisterPaymentDto dto, CancellationToken cancellationToken = default);
}
