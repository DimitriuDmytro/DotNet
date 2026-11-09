namespace Platform.ServiceDefaults;

using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;

/// <summary>
/// Middleware для перехоплення, санітизації та наскрізного прокидання X-Correlation-Id між мікросервісами.
/// Забезпечує захист від Log Injection, надійний boundary-лог та зв'язок з OpenTelemetry Activity.
/// </summary>
public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    public const string CorrelationIdHeader = "X-Correlation-Id";

    private static readonly Regex SafeIdPattern = new("^[A-Za-z0-9\\-_]{1,64}$", RegexOptions.Compiled);
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var rawCorrelationId = context.Request.Headers[HeaderName].FirstOrDefault();
        string correlationId;

        if (string.IsNullOrWhiteSpace(rawCorrelationId) || !SafeIdPattern.IsMatch(rawCorrelationId))
        {
            correlationId = Guid.NewGuid().ToString("D");
            _logger.LogInformation("Generated new CorrelationId {CorrelationId}", correlationId);
        }
        else
        {
            correlationId = rawCorrelationId;
        }

        // Записуємо валідований ID назад у заголовки запиту
        context.Request.Headers[HeaderName] = correlationId;

        // Присвоюємо заголовок перед надсиланням відповіді клієнту (гарантовано одне значення)
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        // Прив'язуємо CorrelationId до активного OTel span (trace зв'язок)
        Activity.Current?.SetTag("correlation.id", correlationId);

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            _logger.LogInformation("Request started {Method} {Path}", context.Request.Method, context.Request.Path);

            var stopwatch = Stopwatch.StartNew();
            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogInformation(
                    "Request finished {Method} {Path} with {StatusCode} in {Elapsed} ms",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
