namespace Bookings.Api.Controllers;

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Bookings.Api.Metrics;
using Bookings.Bll.DTOs;
using Bookings.Bll.Interfaces;

/// <summary>
/// Контролер для керування записами на детейлінг та їхнім життєвим циклом.
/// Тонкий шар презентації з підтримкою структурованого логування, метрик та трасування OpenTelemetry.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private static readonly ActivitySource Source = new("Bookings.Api");
    private readonly IAppointmentService _appointmentService;
    private readonly BookingMetrics _metrics;
    private readonly ILogger<AppointmentsController> _logger;

    public AppointmentsController(
        IAppointmentService appointmentService,
        BookingMetrics metrics,
        ILogger<AppointmentsController> logger)
    {
        _appointmentService = appointmentService;
        _metrics = metrics;
        _logger = logger;
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentDto>> GetById(long id, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        using var activity = Source.StartActivity("AppointmentsController.GetById");
        activity?.SetTag("appointment.id", id);

        _logger.LogInformation("AppointmentsController: Запит запису #{AppointmentId}", id);
        try
        {
            var appointment = await _appointmentService.GetByIdAsync(id, cancellationToken);
            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "get_by_id", "success");
            return Ok(appointment);
        }
        catch
        {
            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "get_by_id", "failure");
            throw;
        }
    }

    [HttpGet("client/{clientId:long}")]
    [ProducesResponseType(typeof(IReadOnlyList<AppointmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<AppointmentDto>>> GetByClientId(
        long clientId,
        [FromQuery] string? status = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        using var activity = Source.StartActivity("AppointmentsController.GetByClientId");
        activity?.SetTag("appointment.client_id", clientId);

        _logger.LogInformation(
            "AppointmentsController: Запит записів клієнта #{ClientId}, статус {Status}, сторінка {PageNumber}",
            clientId,
            status ?? "all",
            pageNumber);

        try
        {
            var appointments = await _appointmentService.GetByClientIdAsync(
                clientId, status, pageNumber, pageSize, cancellationToken);
            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "get_by_client_id", "success");
            return Ok(appointments);
        }
        catch
        {
            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "get_by_client_id", "failure");
            throw;
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentDto>> Create([FromBody] CreateAppointmentDto dto, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        using var activity = Source.StartActivity("AppointmentsController.Create");
        activity?.SetTag("appointment.client_id", dto.ClientId);

        _logger.LogInformation("AppointmentsController: Створення нового запису для клієнта #{ClientId}", dto.ClientId);
        try
        {
            var created = await _appointmentService.CreateAsync(dto, cancellationToken);
            activity?.SetTag("appointment.id", created.Id);

            _metrics.RecordBookingCreated();
            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "create", "success");

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch
        {
            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "create", "failure");
            throw;
        }
    }

    [HttpPatch("{id:long}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeStatus(long id, [FromBody] ChangeStatusDto dto, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        using var activity = Source.StartActivity("AppointmentsController.ChangeStatus");
        activity?.SetTag("appointment.id", id);
        activity?.SetTag("appointment.new_status", dto.NewStatus);

        _logger.LogInformation("AppointmentsController: Зміна статусу запису #{AppointmentId} на '{NewStatus}'", id, dto.NewStatus);
        try
        {
            await _appointmentService.ChangeStatusAsync(id, dto, cancellationToken);
            _metrics.RecordStatusChanged("Unknown", dto.NewStatus);
            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "change_status", "success");
            return NoContent();
        }
        catch
        {
            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "change_status", "failure");
            throw;
        }
    }

    [HttpPost("{id:long}/deliver")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeliverCar(long id, [FromQuery] string? changedBy = null, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        using var activity = Source.StartActivity("AppointmentsController.DeliverCar");
        activity?.SetTag("appointment.id", id);

        _logger.LogInformation("AppointmentsController: Видача автомобіля для запису #{AppointmentId}", id);
        try
        {
            await _appointmentService.DeliverCarAsync(id, changedBy, cancellationToken);
            _metrics.RecordStatusChanged("ReadyForPickup", "Completed");
            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "deliver", "success");
            return NoContent();
        }
        catch
        {
            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "deliver", "failure");
            throw;
        }
    }

    [HttpPost("{id:long}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(long id, [FromQuery] string? changedBy = null, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        using var activity = Source.StartActivity("AppointmentsController.Cancel");
        activity?.SetTag("appointment.id", id);

        _logger.LogInformation("AppointmentsController: Скасування запису #{AppointmentId}", id);
        try
        {
            await _appointmentService.CancelAsync(id, changedBy, cancellationToken);
            _metrics.RecordBookingCancelled();
            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "cancel", "success");
            return NoContent();
        }
        catch
        {
            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "cancel", "failure");
            throw;
        }
    }

    [HttpGet("{id:long}/history")]
    [ProducesResponseType(typeof(IReadOnlyList<StatusHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<StatusHistoryDto>>> GetStatusHistory(long id, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        using var activity = Source.StartActivity("AppointmentsController.GetStatusHistory");
        activity?.SetTag("appointment.id", id);

        _logger.LogInformation("AppointmentsController: Отримання історії статусів для запису #{AppointmentId}", id);
        try
        {
            var history = await _appointmentService.GetStatusHistoryAsync(id, cancellationToken);
            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "get_history", "success");
            return Ok(history);
        }
        catch
        {
            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "get_history", "failure");
            throw;
        }
    }
}
