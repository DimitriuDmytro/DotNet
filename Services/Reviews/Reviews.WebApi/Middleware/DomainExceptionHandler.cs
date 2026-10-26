namespace Reviews.WebApi.Middleware;

using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Reviews.Domain.Exceptions;

/// <summary>
/// Глобальний обробник винятків через ASP.NET Core IExceptionHandler (Критерій 13).
/// Зіставляє доменні винятки, валідацію та помилки MongoDB у формат RFC 7807 ProblemDetails.
/// Забезпечує рівно один структурований запис у лог для кожного винятку.
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
        var (statusCode, title, detail, type) = exception switch
        {
            NotFoundException notFound => (
                StatusCodes.Status404NotFound,
                "Resource Not Found",
                notFound.Message,
                "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4"),

            ValidationException validation => (
                StatusCodes.Status400BadRequest,
                "Validation Error",
                "Один або декілька параметрів запиту не пройшли валідацію.",
                "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1"),

            DomainException domain => (
                StatusCodes.Status400BadRequest,
                "Domain Rule Violation",
                domain.Message,
                "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1"),

            ConflictException conflict => (
                StatusCodes.Status409Conflict,
                "Conflict",
                conflict.Message,
                "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.8"),

            MongoWriteException mongoWrite when mongoWrite.WriteError?.Category == ServerErrorCategory.DuplicateKey => (
                StatusCodes.Status409Conflict,
                "Duplicate Key Conflict",
                "Запис з такими унікальними полями вже існує у базі даних MongoDB.",
                "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.8"),

            MongoWriteException mongoWrite => (
                StatusCodes.Status400BadRequest,
                "Database Write Error",
                mongoWrite.Message,
                "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1"),

            MongoConnectionException connection => (
                StatusCodes.Status503ServiceUnavailable,
                "Database Service Unavailable",
                "Сервер бази даних MongoDB тимчасово недоступний. Спробуйте пізніше.",
                "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.4"),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                _environment.IsDevelopment()
                    ? exception.Message
                    : "Сталася непередбачена внутрішня помилка сервера.",
                "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1")
        };

        if (statusCode >= 500)
        {
            _logger.LogError(exception, "Необроблена помилка сервера {StatusCode}: {Message}", statusCode, exception.Message);
        }
        else
        {
            _logger.LogWarning("Клієнтська помилка {StatusCode}: {Title} - {Detail}", statusCode, title, detail);
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        ProblemDetails problemDetails;

        if (exception is ValidationException validationEx)
        {
            var errors = validationEx.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            problemDetails = new HttpValidationProblemDetails(errors)
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Type = type,
                Instance = httpContext.Request.Path
            };
        }
        else
        {
            problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Type = type,
                Instance = httpContext.Request.Path
            };
        }

        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
