namespace Reviews.WebApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

/// <summary>
/// Ендпоінти для симуляції збоїв та тестування політик відмовостійкості (Resilience, Retry, Circuit Breaker, Timeout).
/// Доступний лише у середовищі Development для перевірки відмовостійкості агрегатора та шлюзу.
/// </summary>
[ApiController]
[Route("api/_test")]
public class TestFailureController : ControllerBase
{
    private readonly ILogger<TestFailureController> _logger;

    public TestFailureController(ILogger<TestFailureController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Симуляція затримки відповіді сервісу для перевірки AttemptTimeout та TotalRequestTimeout.
    /// </summary>
    [HttpGet("delay")]
    public async Task<IActionResult> Delay([FromQuery] int ms = 3000, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Test failure injection: delaying response for {DelayMs}ms", ms);
        await Task.Delay(ms, cancellationToken);
        return Ok(new { delayedMs = ms, status = "ok", message = $"Response delayed by {ms}ms" });
    }

    /// <summary>
    /// Симуляція випадкових помилок (503 Service Unavailable) для перевірки Retry та Circuit Breaker.
    /// </summary>
    [HttpGet("fail")]
    public IActionResult Fail([FromQuery] double rate = 1.0)
    {
        _logger.LogWarning("Test failure injection: failure requested with failure rate {Rate}", rate);

        if (Random.Shared.NextDouble() < rate)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                error = "Simulated Service Unavailable",
                rate,
                status = 503
            });
        }

        return Ok(new { status = "ok", message = "Service responded successfully", rate });
    }
}
