namespace Bookings.Dal.Interfaces;

using Bookings.Domain.Models;

/// <summary>
/// Репозиторій для роботи з фінансовими платежами (Dapper + usp_RegisterPayment).
/// Успадковує IGenericRepository<Payment> без дублювання базових CRUD-методів.
/// </summary>
public interface IPaymentRepository : IGenericRepository<Payment>
{
    Task<IReadOnlyList<Payment>> GetByAppointmentIdAsync(long appointmentId, CancellationToken cancellationToken = default);
    Task<Payment> RegisterPaymentViaProcedureAsync(long appointmentId, decimal amount, string method, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalPaidForAppointmentAsync(long appointmentId, CancellationToken cancellationToken = default);
}
