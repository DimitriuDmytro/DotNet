namespace Platform.Aggregator.Clients;

using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Platform.Aggregator.Dtos;

public class CatalogClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CatalogClient> _logger;

    public CatalogClient(HttpClient httpClient, ILogger<CatalogClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ProductDto?> GetProductByIdAsync(long id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Calling catalog-api for product #{ProductId}", id);
        return await _httpClient.GetFromJsonAsync<ProductDto>(
            $"/api/services/{id}",
            cancellationToken);
    }
}
