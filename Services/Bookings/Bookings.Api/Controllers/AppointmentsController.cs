namespace Bookings.Api.Controllers;

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Bookings.Bll.DTOs;
using Bookings.Bll.Interfaces;

/// <summary>
/// Контролер для керування записами на детейлінг та їхнім життєвим циклом.
/// Тонкий шар презентації з підтримкою структурованого логування та трасування OpenTelemetry.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private static readonly ActivitySource Source = new("Bookings.Api");
    private readonly IAppointmentService _appointmentService;
    private readonly ILogger<AppointmentsController> _logger;

    public AppointmentsController(
        IAppointmentService appointmentService,
        ILogger<AppointmentsController> logger)
    {
        _appointmentService = appointmentService;
        _logger = logger;
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentDto>> GetById(long id, CancellationToken cancellationToken = default)
    {
        using var activity = Source.StartActivity("AppointmentsController.GetById");
        activity?.SetTag("appointment.id", id);

        _logger.LogInformation("AppointmentsController: Запит запису #{AppointmentId}", id);
        var appointment = await _appointmentService.GetByIdAsync(id, cancellationToken);
        return Ok(appointment);
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
        using var activity = Source.StartActivity("AppointmentsController.GetByClientId");
        activity?.SetTag("appointment.client_id", clientId);

        _logger.LogInformation(
            "AppointmentsController: Запит записів клієнта #{ClientId}, статус {Status}, сторінка {PageNumber}",
            clientId,
            status ?? "all",
            pageNumber);

        var appointments = await _appointmentService.GetByClientIdAsync(
            clientId, status, pageNumber, pageSize, cancellationToken);
        return Ok(appointments);
    }

    [HttpPost]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentDto>> Create([FromBody] CreateAppointmentDto dto, CancellationToken cancellationToken = default)
    {
        using var activity = Source.StartActivity("AppointmentsController.Create");
        activity?.SetTag("appointment.client_id", dto.ClientId);

        _logger.LogInformation("AppointmentsController: Створення нового запису для клієнта #{ClientId}", dto.ClientId);
        var created = await _appointmentService.CreateAsync(dto, cancellationToken);
        activity?.SetTag("appointment.id", created.Id);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPatch("{id:long}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeStatus(long id, [FromBody] ChangeStatusDto dto, CancellationToken cancellationToken = default)
    {
        using var activity = Source.StartActivity("AppointmentsController.ChangeStatus");
        activity?.SetTag("appointment.id", id);
        activity?.SetTag("appointment.new_status", dto.NewStatus);

        _logger.LogInformation("AppointmentsController: Зміна статусу запису #{AppointmentId} на '{NewStatus}'", id, dto.NewStatus);
        await _appointmentService.ChangeStatusAsync(id, dto, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:long}/deliver")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeliverCar(long id, [FromQuery] string? changedBy = null, CancellationToken cancellationToken = default)
    {
        using var activity = Source.StartActivity("AppointmentsController.DeliverCar");
        activity?.SetTag("appointment.id", id);

        _logger.LogInformation("AppointmentsController: Видача автомобіля для запису #{AppointmentId}", id);
        await _appointmentService.DeliverCarAsync(id, changedBy, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:long}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(long id, [FromQuery] string? changedBy = null, CancellationToken cancellationToken = default)
    {
        using var activity = Source.StartActivity("AppointmentsController.Cancel");
        activity?.SetTag("appointment.id", id);

        _logger.LogInformation("AppointmentsController: Скасування запису #{AppointmentId}", id);
        await _appointmentService.CancelAsync(id, changedBy, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:long}/history")]
    [ProducesResponseType(typeof(IReadOnlyList<StatusHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<StatusHistoryDto>>> GetStatusHistory(long id, CancellationToken cancellationToken = default)
    {
        using var activity = Source.StartActivity("AppointmentsController.GetStatusHistory");
        activity?.SetTag("appointment.id", id);

        _logger.LogInformation("AppointmentsController: Отримання історії статусів для запису #{AppointmentId}", id);
        var history = await _appointmentService.GetStatusHistoryAsync(id, cancellationToken);
        return Ok(history);
    }
}
