using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Security.Permissions;

namespace Services.Payments.Api.Controllers;

[ApiController]
[Route("api/payments")]
[Route("payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private static readonly List<PaymentRecord> _payments = new()
    {
        new(Guid.NewGuid(), "ORD-1001", 150.00m, "USD", "Completed", DateTime.UtcNow.AddHours(-2)),
        new(Guid.NewGuid(), "ORD-1002", 75.50m, "USD", "Completed", DateTime.UtcNow.AddHours(-1))
    };

    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(ILogger<PaymentsController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    [RequirePermission("payments:read")]
    public IActionResult GetPayments()
    {
        _logger.LogInformation("Retrieved {Count} payments", _payments.Count);
        return Ok(_payments);
    }

    [HttpPost("process")]
    [RequirePermission("payments:process")]
    public IActionResult ProcessPayment([FromBody] ProcessPaymentRequest request)
    {
        var record = new PaymentRecord(
            Guid.NewGuid(),
            request.OrderNumber,
            request.Amount,
            request.Currency ?? "USD",
            "Completed",
            DateTime.UtcNow);

        _payments.Add(record);
        _logger.LogInformation("Processed payment {PaymentId} for order {OrderNumber} amount {Amount}",
            record.Id, record.OrderNumber, record.Amount);

        return Ok(record);
    }

    [HttpPost("refund")]
    [RequirePermission("payments:refund")]
    public IActionResult RefundPayment([FromBody] RefundPaymentRequest request)
    {
        var existing = _payments.FirstOrDefault(p => p.Id == request.PaymentId);
        if (existing == null)
        {
            return NotFound(new { message = "Payment not found." });
        }

        var refunded = existing with { Status = "Refunded" };
        _payments.Remove(existing);
        _payments.Add(refunded);

        _logger.LogInformation("Refunded payment {PaymentId} amount {Amount}", refunded.Id, refunded.Amount);
        return Ok(refunded);
    }
}

public record PaymentRecord(Guid Id, string OrderNumber, decimal Amount, string Currency, string Status, DateTime CreatedAt);
public record ProcessPaymentRequest(string OrderNumber, decimal Amount, string? Currency);
public record RefundPaymentRequest(Guid PaymentId, string? Reason);
