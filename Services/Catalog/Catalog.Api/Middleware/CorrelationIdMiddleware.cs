namespace Catalog.Api.Middleware;

using Serilog.Context;

/// <summary>
/// Middleware для збагачення контексту логування Serilog властивостями CorrelationId, RequestPath та UserId (Критерій 13).
/// Перевіряє вхідний заголовок X-Correlation-Id або генерує новий Guid, повертаючи його у відповіді.
/// </summary>
public class CorrelationIdMiddleware
{
    public const string CorrelationIdHeader = "X-Correlation-Id";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N");
        }

        context.Response.Headers[CorrelationIdHeader] = correlationId;

        var userId = context.User?.Identity?.Name ?? "anonymous";

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("RequestPath", context.Request.Path))
        using (LogContext.PushProperty("UserId", userId))
        {
            await _next(context);
        }
    }
}
