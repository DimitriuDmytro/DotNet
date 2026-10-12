namespace Bookings.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using Bookings.Bll.DTOs;
using Bookings.Bll.Interfaces;

/// <summary>
/// Контролер для керування записами на детейлінг та їхнім життєвим циклом.
/// Тонкий шар презентації без бізнес-логіки та SQL.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;

    public AppointmentsController(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken = default)
    {
        var appointment = await _appointmentService.GetByIdAsync(id, cancellationToken);
        return Ok(appointment);
    }

    [HttpGet("client/{clientId:long}")]
    [ProducesResponseType(typeof(IReadOnlyList<AppointmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByClientId(
        long clientId,
        [FromQuery] string? status = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var appointments = await _appointmentService.GetByClientIdAsync(
            clientId, status, pageNumber, pageSize, cancellationToken);
        return Ok(appointments);
    }

    [HttpPost]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentDto dto, CancellationToken cancellationToken = default)
    {
        var created = await _appointmentService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPatch("{id:long}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeStatus(long id, [FromBody] ChangeStatusDto dto, CancellationToken cancellationToken = default)
    {
        await _appointmentService.ChangeStatusAsync(id, dto, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:long}/deliver")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeliverCar(long id, [FromQuery] string? changedBy = null, CancellationToken cancellationToken = default)
    {
        await _appointmentService.DeliverCarAsync(id, changedBy, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:long}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(long id, [FromQuery] string? changedBy = null, CancellationToken cancellationToken = default)
    {
        await _appointmentService.CancelAsync(id, changedBy, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:long}/history")]
    [ProducesResponseType(typeof(IReadOnlyList<StatusHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatusHistory(long id, CancellationToken cancellationToken = default)
    {
        var history = await _appointmentService.GetStatusHistoryAsync(id, cancellationToken);
        return Ok(history);
    }
}
