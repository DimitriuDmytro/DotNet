namespace Bookings.Tests;

using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Bookings.Api.Middleware;
using Bookings.Domain.Exceptions;

/// <summary>
/// Тести глобального обробника помилок DomainExceptionHandler (відповідність RFC 7807 ProblemDetails).
/// </summary>
public class DomainExceptionHandlerTests
{
    private readonly DomainExceptionHandler _handler;

    public DomainExceptionHandlerTests()
    {
        _handler = new DomainExceptionHandler(NullLogger<DomainExceptionHandler>.Instance);
    }

    [Theory]
    [InlineData(typeof(ValidationException), StatusCodes.Status400BadRequest)]
    [InlineData(typeof(NotFoundException), StatusCodes.Status404NotFound)]
    [InlineData(typeof(BusinessConflictException), StatusCodes.Status409Conflict)]
    [InlineData(typeof(InvalidOperationException), StatusCodes.Status500InternalServerError)]
    public async Task TryHandleAsync_ShouldMapDomainExceptionsToExpectedHttpStatusCodes(Type exceptionType, int expectedStatusCode)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/appointments/1";
        context.Response.Body = new MemoryStream();

        var exception = (Exception)Activator.CreateInstance(exceptionType, "Тестове повідомлення про помилку")!;

        // Act
        var handled = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        Assert.True(handled);
        Assert.Equal(expectedStatusCode, context.Response.StatusCode);
        Assert.StartsWith("application/problem+json", context.Response.ContentType);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var problem = await JsonSerializer.DeserializeAsync<ProblemDetails>(
            context.Response.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(problem);
        Assert.Equal(expectedStatusCode, problem!.Status);
        Assert.Equal("Тестове повідомлення про помилку", problem.Detail);
        Assert.Equal("/api/appointments/1", problem.Instance);
    }
}
