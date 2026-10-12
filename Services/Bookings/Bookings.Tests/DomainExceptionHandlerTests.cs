namespace Bookings.Tests;

using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Bookings.Api.Middleware;
using Bookings.Domain.Exceptions;

/// <summary>
/// Тести глобального обробника помилок DomainExceptionHandler (відповідність RFC 7807 ProblemDetails,
/// наявність traceId та приховування внутрішніх деталей при 500 Internal Server Error).
/// </summary>
public class DomainExceptionHandlerTests
{
    private readonly DomainExceptionHandler _handler;

    public DomainExceptionHandlerTests()
    {
        _handler = new DomainExceptionHandler(NullLogger<DomainExceptionHandler>.Instance);
    }

    [Theory]
    [InlineData(typeof(ValidationException), StatusCodes.Status400BadRequest, true)]
    [InlineData(typeof(NotFoundException), StatusCodes.Status404NotFound, true)]
    [InlineData(typeof(BusinessConflictException), StatusCodes.Status409Conflict, true)]
    [InlineData(typeof(InvalidOperationException), StatusCodes.Status500InternalServerError, false)]
    public async Task TryHandleAsync_ShouldMapDomainExceptionsToExpectedHttpStatusCodes(
        Type exceptionType,
        int expectedStatusCode,
        bool shouldExposeExceptionMessage)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.TraceIdentifier = "test-trace-id-12345";
        context.Request.Path = "/api/appointments/1";
        context.Response.Body = new MemoryStream();

        var secretOrDomainMessage = "Секретне або доменне повідомлення про помилку";
        var exception = (Exception)Activator.CreateInstance(exceptionType, secretOrDomainMessage)!;

        // Act
        var handled = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        Assert.True(handled);
        Assert.Equal(expectedStatusCode, context.Response.StatusCode);
        Assert.StartsWith("application/problem+json", context.Response.ContentType);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var jsonDoc = await JsonDocument.ParseAsync(context.Response.Body);
        var root = jsonDoc.RootElement;

        Assert.Equal(expectedStatusCode, root.GetProperty("status").GetInt32());
        Assert.Equal("/api/appointments/1", root.GetProperty("instance").GetString());

        // Перевірка наявності traceId у ProblemDetails.Extensions
        Assert.True(root.TryGetProperty("traceId", out var traceIdProp));
        Assert.False(string.IsNullOrWhiteSpace(traceIdProp.GetString()));

        // Перевірка відсутності витоку сирого повідомлення для 500
        var detail = root.GetProperty("detail").GetString();
        if (shouldExposeExceptionMessage)
        {
            Assert.Equal(secretOrDomainMessage, detail);
        }
        else
        {
            Assert.DoesNotContain(secretOrDomainMessage, detail);
        }
    }
}
