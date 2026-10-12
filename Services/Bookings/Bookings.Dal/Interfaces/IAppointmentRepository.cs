namespace Bookings.Dal.Interfaces;

using Bookings.Domain.Models;

/// <summary>
/// Репозиторій записів на детейлінг (реалізація через Dapper, Multi-mapping, TVP та збережені процедури).
/// Успадковує IGenericRepository<Appointment> і містить лише специфічні запити.
/// </summary>
public interface IAppointmentRepository : IGenericRepository<Appointment>
{
    /// <summary>
    /// Отримання повного агрегата Appointment разом із деталями авто (1:1) та списком замовлених послуг (1:N).
    /// </summary>
    Task<Appointment?> GetDetailedByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Отримання записів клієнта з пагінацією та фільтрацією за статусом (через процедуру dbo.usp_GetClientAppointments).
    /// </summary>
    Task<IReadOnlyList<Appointment>> GetByClientIdAsync(
        long clientId,
        string? status = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Створення нового запису через збережену процедуру dbo.usp_CreateAppointment з передачею TVP
    /// (включно зі знімком ServiceName та UnitPrice, сформованим у BLL) та обчисленим TotalAmount.
    /// Повертає іменований record CreateAppointmentResult.
    /// </summary>
    Task<CreateAppointmentResult> CreateViaProcedureAsync(
        long clientId,
        DateTime scheduledAt,
        string carBrand,
        string carModel,
        string plateNumber,
        string? comment,
        string createdBy,
        decimal totalAmount,
        IReadOnlyList<AppointmentItem> items,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Атомарна зміна статусу запису через процедуру dbo.usp_ChangeStatus з перевіркою матриці переходів.
    /// </summary>
    Task ChangeStatusViaProcedureAsync(
        long appointmentId,
        string newStatus,
        string changedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Видача автомобіля клієнту через процедуру dbo.usp_DeliverCar (перевірка Ready + повної оплати).
    /// </summary>
    Task DeliverCarViaProcedureAsync(
        long appointmentId,
        string changedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Скасування запису через процедуру dbo.usp_CancelAppointment (лише з Booked або CarReceived).
    /// </summary>
    Task CancelViaProcedureAsync(
        long appointmentId,
        string changedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Отримання повної історії статусів для запису.
    /// </summary>
    Task<IReadOnlyList<StatusHistoryEntry>> GetStatusHistoryAsync(
        long appointmentId,
        CancellationToken cancellationToken = default);
}
