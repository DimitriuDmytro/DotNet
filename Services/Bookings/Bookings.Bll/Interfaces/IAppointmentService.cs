namespace Bookings.Bll.Interfaces;

using Bookings.Bll.DTOs;

public interface IAppointmentService
{
    Task<AppointmentDto> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AppointmentDto>> GetByClientIdAsync(long clientId, string? status = null, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<AppointmentDto> CreateAsync(CreateAppointmentDto dto, CancellationToken cancellationToken = default);
    Task ChangeStatusAsync(long id, ChangeStatusDto dto, CancellationToken cancellationToken = default);
    Task DeliverCarAsync(long id, string? changedBy = null, CancellationToken cancellationToken = default);
    Task CancelAsync(long id, string? changedBy = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StatusHistoryDto>> GetStatusHistoryAsync(long id, CancellationToken cancellationToken = default);
}
