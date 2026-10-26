namespace Reviews.Tests.Api;

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson;
using NSubstitute;
using Reviews.Application.Reviews.Commands.CreateReview;
using Reviews.Infrastructure.Seeding;

public class ReviewsControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly IReviewRepository _mockRepository = Substitute.For<IReviewRepository>();
    private readonly IReviewQueries _mockQueries = Substitute.For<IReviewQueries>();

    public ReviewsControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("MongoDb:RunStartupTasks", "false");
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // Заміна репозиторію та запитів на моки для незалежності від живого MongoDB демона
                var repoDesc = services.FirstOrDefault(d => d.ServiceType == typeof(IReviewRepository));
                if (repoDesc != null) services.Remove(repoDesc);
                services.AddScoped(_ => _mockRepository);

                var queryDesc = services.FirstOrDefault(d => d.ServiceType == typeof(IReviewQueries));
                if (queryDesc != null) services.Remove(queryDesc);
                services.AddScoped(_ => _mockQueries);

                // Вимкнення реального сідінгу та hosted service для тестів
                var hostedServices = services.Where(d => d.ServiceType == typeof(IHostedService)).ToList();
                foreach (var hs in hostedServices) services.Remove(hs);

                var seederDesc = services.FirstOrDefault(d => d.ServiceType == typeof(IDataSeeder));
                if (seederDesc != null) services.Remove(seederDesc);
                var mockSeeder = Substitute.For<IDataSeeder>();
                services.AddScoped(_ => mockSeeder);
            });
        });
    }

    [Fact]
    public async Task GetReviews_ReturnsOkAndPagedResult()
    {
        // Arrange
        var pagedResult = new PagedResult<ReviewListDto>(
            new List<ReviewListDto>
            {
                new ReviewListDto("670900000000000000000301", 7, "Кераміка 9H", 5, "Супер!", "Олександр", 2, DateTime.UtcNow, 1)
            },
            1, 1, 20);

        _mockQueries.GetPagedAsync(null, 1, 20, Arg.Any<CancellationToken>())
            .Returns(pagedResult);

        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/reviews?page=1&pageSize=20");

        // Assert (Критерій 12)
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<PagedResult<ReviewListDto>>();
        Assert.NotNull(content);
        Assert.Equal(1, content.TotalCount);
        Assert.Single(content.Items);
    }

    [Fact]
    public async Task GetReviewById_WhenFound_ReturnsOkAndETagHeader()
    {
        // Arrange
        var id = "670900000000000000000301";
        var detailDto = new ReviewDetailDto(
            Id: id,
            ServiceId: 7,
            ServiceName: "Кераміка 9H",
            AppointmentId: 1,
            DiscussionId: null,
            Rating: 5,
            Title: "Блиск",
            Text: "Дійсно супер якість виконання",
            VerifiedVisit: true,
            Author: new AuthorDto(1, "Олександр", "BMW X5"),
            Comments: Array.Empty<ReviewCommentDto>(),
            PhotosBefore: Array.Empty<string>(),
            PhotosAfter: Array.Empty<string>(),
            VideoUrl: null,
            Pros: Array.Empty<string>(),
            Cons: Array.Empty<string>(),
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: null,
            Version: 1);

        _mockQueries.GetByIdAsync(ObjectId.Parse(id), Arg.Any<CancellationToken>())
            .Returns(detailDto);

        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/reviews/{id}");

        // Assert (Критерій 12 - ETag header)
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("ETag"));
        Assert.Equal("\"1\"", response.Headers.ETag?.Tag);
    }

    [Fact]
    public async Task GetReviewById_WhenNotFound_Returns404WithProblemDetails()
    {
        // Arrange
        var id = "670900000000000000000399";
        _mockQueries.GetByIdAsync(ObjectId.Parse(id), Arg.Any<CancellationToken>())
            .Returns((ReviewDetailDto?)null);

        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/reviews/{id}");

        // Assert (Критерій 12, 13)
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task CreateReview_WithInvalidData_Returns400WithValidationProblemDetails()
    {
        // Arrange
        var client = _factory.CreateClient();
        var invalidCommand = new CreateReviewCommand(
            ServiceId: 0, // Невалідно (має бути > 0)
            ServiceName: "",
            AppointmentId: null,
            DiscussionId: null,
            Rating: 10, // Невалідно (має бути 1..5)
            Title: "a",
            Text: "b",
            VerifiedVisit: false,
            AuthorClientId: 0,
            AuthorFullName: "",
            AuthorCarInfo: null);

        // Act
        var response = await client.PostAsJsonAsync("/api/reviews", invalidCommand);

        // Assert (Критерій 8, 13)
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
