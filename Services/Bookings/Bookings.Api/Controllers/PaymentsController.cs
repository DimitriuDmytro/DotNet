namespace Bookings.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using Bookings.Bll.DTOs;
using Bookings.Bll.Interfaces;

/// <summary>
/// Контролер для реєстрації та перегляду оплат клієнтів за записами.
/// </summary>
[ApiController]
[Route("api/appointments/{appointmentId:long}/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByAppointmentId(long appointmentId, CancellationToken cancellationToken = default)
    {
        var payments = await _paymentService.GetByAppointmentIdAsync(appointmentId, cancellationToken);
        return Ok(payments);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterPayment(
        long appointmentId,
        [FromBody] RegisterPaymentDto dto,
        CancellationToken cancellationToken = default)
    {
        await _paymentService.RegisterPaymentAsync(appointmentId, dto, cancellationToken);
        return NoContent();
    }
}
