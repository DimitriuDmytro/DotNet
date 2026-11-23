using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;

namespace Platform.Aggregator.Security;

public interface IIdentityServerTokenService
{
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}

public class IdentityServerTokenService : IIdentityServerTokenService
{
    private const string CacheKey = "Aggregator_IdentityServer_Token";
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IdentityServerTokenService> _logger;

    public IdentityServerTokenService(
        HttpClient httpClient,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<IdentityServerTokenService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue<string>(CacheKey, out var cachedToken) && !string.IsNullOrEmpty(cachedToken))
        {
            return cachedToken;
        }

        var authority = _configuration["IdentityServer:Authority"] ?? "http://localhost:5001";
        var clientId = _configuration["IdentityServer:ClientId"] ?? "aggregator_service";
        var clientSecret = _configuration["IdentityServer:ClientSecret"] ?? "aggregator-secret";
        var scope = _configuration["IdentityServer:Scope"] ?? "orders.read products.read";

        try
        {
            var tokenEndpoint = $"{authority.TrimEnd('/')}/connect/token";
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["grant_type"] = "client_credentials",
                ["scope"] = scope
            });

            _logger.LogInformation("Requesting M2M client credentials token from {TokenEndpoint} for client {ClientId}",
                tokenEndpoint, clientId);

            var response = await _httpClient.PostAsync(tokenEndpoint, content, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var tokenResult = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);
                if (tokenResult?.AccessToken != null)
                {
                    var cacheDuration = TimeSpan.FromSeconds(Math.Max(60, tokenResult.ExpiresIn - 60));
                    _cache.Set(CacheKey, tokenResult.AccessToken, cacheDuration);
                    _logger.LogInformation("M2M client credentials token acquired and cached for {DurationSeconds}s", cacheDuration.TotalSeconds);
                    return tokenResult.AccessToken;
                }
            }
            else
            {
                _logger.LogWarning("Failed to obtain M2M token: HTTP {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exception while requesting IdentityServer M2M token. Proceeding with fallback/anonymous.");
        }

        return null;
    }

    private class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }
    }
}
