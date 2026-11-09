namespace Bookings.Bll.Caching;

using Bookings.Bll.DTOs;
using Bookings.Bll.Interfaces;
using Bookings.Bll.Services;
using Microsoft.Extensions.Logging;

public sealed class CachedAppointmentService : IAppointmentService
{
    private readonly AppointmentService _inner;
    private readonly IBookingsTwoLevelCache _cache;
    private readonly ILogger<CachedAppointmentService> _logger;

    public CachedAppointmentService(
        AppointmentService inner,
        IBookingsTwoLevelCache cache,
        ILogger<CachedAppointmentService> logger)
    {
        _inner = inner;
        _cache = cache;
        _logger = logger;
    }

    public async Task<AppointmentDto> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var key = BookingsCacheKeys.Appointment(id);
        var cached = await _cache.GetOrCreateAsync(
            key,
            async ct => await _inner.GetByIdAsync(id, ct),
            BookingsCacheTtl.Appointment,
            cancellationToken);

        return cached!;
    }

    public async Task<IReadOnlyList<AppointmentDto>> GetByClientIdAsync(long clientId, string? status = null, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var key = BookingsCacheKeys.ClientAppointments(clientId, status, pageNumber, pageSize);
        var cached = await _cache.GetOrCreateAsync(
            key,
            async ct => await _inner.GetByClientIdAsync(clientId, status, pageNumber, pageSize, ct),
            BookingsCacheTtl.ClientAppointments,
            cancellationToken);

        return cached ?? Array.Empty<AppointmentDto>();
    }

    public async Task<AppointmentDto> CreateAsync(CreateAppointmentDto dto, CancellationToken cancellationToken = default)
    {
        var result = await _inner.CreateAsync(dto, cancellationToken);
        await InvalidateAppointmentAsync(result.Id, $"Appointment created {result.Id}", cancellationToken);
        return result;
    }

    public async Task ChangeStatusAsync(long id, ChangeStatusDto dto, CancellationToken cancellationToken = default)
    {
        await _inner.ChangeStatusAsync(id, dto, cancellationToken);
        await InvalidateAppointmentAsync(id, $"Appointment status changed to {dto.NewStatus}", cancellationToken);
    }

    public async Task DeliverCarAsync(long id, string? changedBy = null, CancellationToken cancellationToken = default)
    {
        await _inner.DeliverCarAsync(id, changedBy, cancellationToken);
        await InvalidateAppointmentAsync(id, "Appointment car delivered", cancellationToken);
    }

    public async Task CancelAsync(long id, string? changedBy = null, CancellationToken cancellationToken = default)
    {
        await _inner.CancelAsync(id, changedBy, cancellationToken);
        await InvalidateAppointmentAsync(id, "Appointment canceled", cancellationToken);
    }

    public async Task<IReadOnlyList<StatusHistoryDto>> GetStatusHistoryAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _inner.GetStatusHistoryAsync(id, cancellationToken);
    }

    private async Task InvalidateAppointmentAsync(long id, string reason, CancellationToken ct)
    {
        await _cache.RemoveAsync(BookingsCacheKeys.Appointment(id), reason, ct);
        _logger.LogInformation("Invalidated cache for appointment {AppointmentId}: {Reason}", id, reason);
    }
}
