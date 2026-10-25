using System.Net;
using System.Net.Http.Json;
using Catalog.Bll.DTOs;
using Catalog.Bll.Interfaces;
using Catalog.Domain.Entities;
using Catalog.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Catalog.Tests;

public class ControllersIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ControllersIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetServices_Returns200Ok_AndIncludesPaginationAndCorrelationHeaders()
    {
        // Arrange
        var serviceMock = Substitute.For<IServiceService>();
        var items = new List<ServiceDto>
        {
            new() { Id = 1, Name = "Полірування", Price = 3000m, DurationMin = 180, CategoryId = 1, CategoryName = "Детейлінг" }
        };
        var pagedResult = new PagedResult<ServiceDto>(items, totalCount: 1, page: 1, pageSize: 20);

        serviceMock.GetPagedAsync(Arg.Any<ServiceQuery>(), Arg.Any<CancellationToken>())
            .Returns(pagedResult);

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped(_ => serviceMock);
            });
        }).CreateClient();

        // Act
        var response = await client.GetAsync("/api/services?page=1&pageSize=20");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Total-Count"));
        Assert.Equal("1", response.Headers.GetValues("X-Total-Count").First());
        Assert.True(response.Headers.Contains("X-Page"));
        Assert.Equal("1", response.Headers.GetValues("X-Page").First());
        Assert.True(response.Headers.Contains("X-Page-Size"));
        Assert.Equal("20", response.Headers.GetValues("X-Page-Size").First());
        Assert.True(response.Headers.Contains("X-Total-Pages"));
        Assert.Equal("1", response.Headers.GetValues("X-Total-Pages").First());
        Assert.True(response.Headers.Contains("X-Correlation-Id"));
    }

    [Fact]
    public async Task CreateService_WithValidData_Returns201Created_AndLocationHeader()
    {
        // Arrange
        var serviceMock = Substitute.For<IServiceService>();
        var created = new ServiceDetailDto
        {
            Id = 42,
            Name = "Керамічне покриття",
            Price = 8000m,
            DurationMin = 240,
            CategoryId = 1,
            CategoryName = "Захист"
        };

        serviceMock.CreateAsync(Arg.Any<CreateServiceDto>(), Arg.Any<CancellationToken>())
            .Returns(created);

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped(_ => serviceMock);
            });
        }).CreateClient();

        var dto = new CreateServiceDto
        {
            CategoryId = 1,
            Name = "Керамічне покриття",
            Price = 8000m,
            DurationMin = 240
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/services", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Contains("/api/Services/42", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task CreateService_WithInvalidData_Returns400ValidationProblemDetails()
    {
        // Arrange
        var client = _factory.CreateClient();
        var invalidDto = new CreateServiceDto
        {
            CategoryId = 0,
            Name = "",
            Price = -50m,
            DurationMin = 0
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/services", invalidDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains(problem!.Errors, e => e.Key.Equals(nameof(CreateServiceDto.Name), StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetServiceById_WhenNotFound_Returns404ProblemDetails()
    {
        // Arrange
        var serviceMock = Substitute.For<IServiceService>();
        serviceMock.GetByIdAsync(999, Arg.Any<CancellationToken>())
            .ThrowsAsync(new EntityNotFoundException(nameof(Service), 999));

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped(_ => serviceMock);
            });
        }).CreateClient();

        // Act
        var response = await client.GetAsync("/api/services/999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(404, problem!.Status);
        Assert.Contains("999", problem.Detail);
    }

    [Fact]
    public async Task CreateService_WhenNameConflicts_Returns409ConflictProblemDetails()
    {
        // Arrange
        var serviceMock = Substitute.For<IServiceService>();
        serviceMock.CreateAsync(Arg.Any<CreateServiceDto>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConflictException("Послуга з такою назвою вже існує"));

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped(_ => serviceMock);
            });
        }).CreateClient();

        var dto = new CreateServiceDto
        {
            CategoryId = 1,
            Name = "Однокрокове полірування",
            Price = 3500m,
            DurationMin = 180
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/services", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(409, problem!.Status);
    }
}
