namespace Catalog.Api.Middleware;

using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Catalog.Domain.Exceptions;

/// <summary>
/// Глобальний обробник винятків IExceptionHandler (Критерій 12).
/// Транслює доменні винятки у RFC 7807 ProblemDetails із traceId та безпечним описом для Production.
/// Виняток логується рівно один раз.
/// </summary>
public class DomainExceptionHandler : IExceptionHandler
{
    private readonly ILogger<DomainExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public DomainExceptionHandler(ILogger<DomainExceptionHandler> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, typeUrl) = exception switch
        {
            ValidationException => (
                StatusCodes.Status400BadRequest,
                "Помилка валідації даних (Validation Error)",
                "https://tools.ietf.org/html/rfc7231#section-6.5.1"),

            EntityNotFoundException => (
                StatusCodes.Status404NotFound,
                "Ресурс не знайдено (Not Found)",
                "https://tools.ietf.org/html/rfc7231#section-6.5.4"),

            ConflictException => (
                StatusCodes.Status409Conflict,
                "Конфлікт бізнес-логіки або унікальності (Conflict)",
                "https://tools.ietf.org/html/rfc7231#section-6.5.8"),

            DomainException => (
                StatusCodes.Status409Conflict,
                "Порушення доменного бізнес-правила (Domain Conflict)",
                "https://tools.ietf.org/html/rfc7231#section-6.5.8"),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Внутрішня помилка сервера (Internal Server Error)",
                "https://tools.ietf.org/html/rfc7231#section-6.6.1")
        };

        // Логування рівно один раз
        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Необроблена помилка під час виконання {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            _logger.LogWarning("Доменний виняток [{StatusCode}]: {Message} на шляху {Path}",
                statusCode, exception.Message, httpContext.Request.Path);
        }

        // Безпечний опис у Production
        var detail = statusCode == StatusCodes.Status500InternalServerError
            ? (_environment.IsDevelopment() ? exception.ToString() : "Виникла внутрішня помилка сервера. Будь ласка, зверніться до адміністратора.")
            : exception.Message;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = typeUrl,
            Instance = httpContext.Request.Path
        };

        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        problemDetails.Extensions["traceId"] = traceId;

        if (httpContext.Response.Headers.TryGetValue(CorrelationIdMiddleware.CorrelationIdHeader, out var correlationId))
        {
            problemDetails.Extensions["correlationId"] = correlationId.ToString();
        }

        if (exception is ValidationException validationEx && validationEx.Errors.Count > 0)
        {
            problemDetails.Extensions["errors"] = validationEx.Errors;
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
