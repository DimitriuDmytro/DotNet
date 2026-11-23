namespace Platform.Aggregator.HealthChecks;

using global::Grpc.Health.V1;
using global::Grpc.Net.ClientFactory;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

/// <summary>
/// Health Check для перевірки доступності gRPC-сервісу Bookings через офіційний gRPC Health Checking Protocol (Критерій 10).
/// </summary>
public sealed class BookingsGrpcHealthCheck : IHealthCheck
{
    private readonly GrpcClientFactory _clientFactory;
    private readonly ILogger<BookingsGrpcHealthCheck> _logger;

    public BookingsGrpcHealthCheck(GrpcClientFactory clientFactory, ILogger<BookingsGrpcHealthCheck> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _clientFactory.CreateClient<Health.HealthClient>("BookingsHealth");
            var response = await client.CheckAsync(
                new HealthCheckRequest { Service = "" },
                deadline: DateTime.UtcNow.AddSeconds(2),
                cancellationToken: cancellationToken);

            return response.Status == HealthCheckResponse.Types.ServingStatus.Serving
                ? HealthCheckResult.Healthy("Bookings gRPC service is SERVING")
                : HealthCheckResult.Unhealthy($"Bookings gRPC service status: {response.Status}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Bookings gRPC health check probe failed");
            return HealthCheckResult.Unhealthy("Bookings gRPC service is unreachable", ex);
        }
    }
}
