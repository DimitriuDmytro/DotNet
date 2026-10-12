namespace Bookings.Tests;

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Bookings.Bll.DTOs;
using Bookings.Bll.Interfaces;
using Bookings.Domain.Exceptions;

/// <summary>
/// Інтеграційні тести HTTP-пайплайну Bookings.Api через WebApplicationFactory.
/// Перевіряють тонкі контролери, серіалізацію JSON та роботу глобального IExceptionHandler.
/// </summary>
public class ControllersIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ControllersIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAppointmentById_WhenExists_ShouldReturn200Ok()
    {
        // Arrange
        var appointmentServiceMock = Substitute.For<IAppointmentService>();
        appointmentServiceMock.GetByIdAsync(100, Arg.Any<CancellationToken>())
            .Returns(new AppointmentDto
            {
                Id = 100,
                ClientId = 1,
                Status = "Booked",
                TotalAmount = 3500m
            });

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped(_ => appointmentServiceMock);
            });
        }).CreateClient();

        // Act
        var response = await client.GetAsync("/api/appointments/100");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<AppointmentDto>();
        Assert.NotNull(dto);
        Assert.Equal(100, dto!.Id);
        Assert.Equal(3500m, dto.TotalAmount);
    }

    [Fact]
    public async Task GetAppointmentById_WhenNotFound_ShouldReturn404ProblemDetails()
    {
        // Arrange
        var appointmentServiceMock = Substitute.For<IAppointmentService>();
        appointmentServiceMock.GetByIdAsync(9999, Arg.Any<CancellationToken>())
            .Throws(new NotFoundException("Запис з ID #9999 не знайдено."));

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped(_ => appointmentServiceMock);
            });
        }).CreateClient();

        // Act
        var response = await client.GetAsync("/api/appointments/9999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(404, problem!.Status);
        Assert.Contains("9999", problem.Detail);
    }

    [Fact]
    public async Task ChangeStatus_WhenBusinessConflict_ShouldReturn409ConflictProblemDetails()
    {
        // Arrange
        var appointmentServiceMock = Substitute.For<IAppointmentService>();
        appointmentServiceMock.ChangeStatusAsync(5, Arg.Any<ChangeStatusDto>(), Arg.Any<CancellationToken>())
            .Throws(new BusinessConflictException("Недопустимий перехід статусу."));

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped(_ => appointmentServiceMock);
            });
        }).CreateClient();

        // Act
        var response = await client.PatchAsJsonAsync("/api/appointments/5/status", new ChangeStatusDto { NewStatus = "Delivered" });

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(409, problem!.Status);
    }
}
