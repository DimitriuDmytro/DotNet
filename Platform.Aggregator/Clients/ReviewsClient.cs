namespace Platform.Aggregator.Clients;

using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Platform.Aggregator.Dtos;

public class ReviewsClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ReviewsClient> _logger;

    public ReviewsClient(HttpClient httpClient, ILogger<ReviewsClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ReviewDto>> GetReviewsByServiceIdAsync(long serviceId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Calling reviews-api for service #{ServiceId}", serviceId);
        var paged = await _httpClient.GetFromJsonAsync<ReviewsPagedApiResponse>(
            $"/api/reviews?serviceId={serviceId}&page=1&pageSize=10",
            cancellationToken);

        if (paged?.Items == null)
        {
            return Array.Empty<ReviewDto>();
        }

        return paged.Items.Select(r => new ReviewDto(
            r.Id,
            r.ServiceId,
            r.ServiceName,
            r.Rating,
            r.Title,
            string.Empty,
            r.AuthorName ?? "Unknown",
            r.CreatedAt)).ToList();
    }
}
