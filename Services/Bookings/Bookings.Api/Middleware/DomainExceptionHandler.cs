namespace Bookings.Api.Middleware;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Bookings.Domain.Exceptions;

/// <summary>
/// Глобальний обробник винятків (IExceptionHandler), який транслює доменні винятки
/// у стандартизовані відповіді RFC 7807 ProblemDetails.
/// </summary>
public class DomainExceptionHandler : IExceptionHandler
{
    private readonly ILogger<DomainExceptionHandler> _logger;

    public DomainExceptionHandler(ILogger<DomainExceptionHandler> logger)
    {
        _logger = logger;
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

            NotFoundException => (
                StatusCodes.Status404NotFound,
                "Ресурс не знайдено (Not Found)",
                "https://tools.ietf.org/html/rfc7231#section-6.5.4"),

            BusinessConflictException => (
                StatusCodes.Status409Conflict,
                "Конфлікт бізнес-логіки або стану (Business Conflict)",
                "https://tools.ietf.org/html/rfc7231#section-6.5.8"),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Внутрішня помилка сервера (Internal Server Error)",
                "https://tools.ietf.org/html/rfc7231#section-6.6.1")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Необроблена помилка під час виконання запиту {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            _logger.LogWarning("Доменний виняток ({StatusCode}): {Message} на шляху {Path}",
                statusCode, exception.Message, httpContext.Request.Path);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message,
            Type = typeUrl,
            Instance = httpContext.Request.Path
        };

        if (exception is ValidationException validationEx && validationEx.Errors.Count > 0)
        {
            problemDetails.Extensions["errors"] = validationEx.Errors;
        }

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);
        return true;
    }
}
