namespace Platform.Aggregator.Controllers;

using Microsoft.AspNetCore.Mvc;
using Platform.Aggregator.Dtos;
using Platform.Aggregator.Services;

[ApiController]
[Route("api/aggregator")]
public class OrdersAggregatorController : ControllerBase
{
    private readonly OrderAggregationService _aggregationService;
    private readonly ILogger<OrdersAggregatorController> _logger;

    public OrdersAggregatorController(
        OrderAggregationService aggregationService,
        ILogger<OrdersAggregatorController> logger)
    {
        _aggregationService = aggregationService;
        _logger = logger;
    }

    /// <summary>
    /// Отримання агрегованої повної картки замовлення з деталями послуг каталогу та відгуками (AG-10).
    /// </summary>
    [HttpGet("orders/{id:long}/full")]
    [ProducesResponseType(typeof(FullOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<FullOrderDto>> GetFullOrder(long id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Запит агрегованої картки замовлення #{AppointmentId}", id);

        try
        {
            var result = await _aggregationService.GetFullOrderAsync(id, cancellationToken);
            if (result == null)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Order Not Found",
                    Detail = $"Замовлення з ID #{id} не знайдено."
                });
            }

            return Ok(result);
        }
        catch (ServiceUnavailableException ex)
        {
            _logger.LogWarning(ex, "Основний сервіс '{ServiceName}' недоступний для замовлення #{AppointmentId}", ex.ServiceName, id);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Service Unavailable",
                Detail = $"Основний сервіс '{ex.ServiceName}' тимчасово недоступний."
            });
        }
    }
}
