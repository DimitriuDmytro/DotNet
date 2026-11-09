namespace Platform.Aggregator.Controllers;

using Microsoft.AspNetCore.Mvc;
using Platform.Aggregator.Dtos;
using Platform.Aggregator.Services;

[ApiController]
[Route("api/aggregator")]
public class OrdersAggregatorController : ControllerBase
{
    private readonly IOrderAggregationService _aggregationService;
    private readonly ILogger<OrdersAggregatorController> _logger;

    public OrdersAggregatorController(
        IOrderAggregationService aggregationService,
        ILogger<OrdersAggregatorController> logger)
    {
        _aggregationService = aggregationService;
        _logger = logger;
    }

    /// <summary>
    /// Отримання агрегованої повної картки замовлення через швидкий gRPC протокол та Redis кеш.
    /// Підтримує оминання кешу через query-параметр ?noCache=true.
    /// </summary>
    [HttpGet("orders/{id:long}/full")]
    [ProducesResponseType(typeof(FullOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<FullOrderDto>> GetFullOrder(
        long id,
        [FromQuery] bool noCache = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Запит агрегованої картки замовлення #{AppointmentId} через gRPC (noCache={NoCache})", id, noCache);

        try
        {
            var result = await _aggregationService.GetFullOrderAsync(id, noCache, cancellationToken);
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

    /// <summary>
    /// Отримання агрегованої повної картки замовлення через HTTP/REST (для порівняльного бенчмарку HTTP vs gRPC).
    /// </summary>
    [HttpGet("orders/{id:long}/full-http")]
    [ProducesResponseType(typeof(FullOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<FullOrderDto>> GetFullOrderHttp(
        long id,
        [FromQuery] bool noCache = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Бенчмарк-запит агрегованої картки #{AppointmentId} через HTTP (noCache={NoCache})", id, noCache);

        try
        {
            var result = await _aggregationService.GetFullOrderHttpAsync(id, noCache, cancellationToken);
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
