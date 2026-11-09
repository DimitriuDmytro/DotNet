namespace Platform.Aggregator.Clients;

using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Platform.Aggregator.Dtos;

public class OrdersClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OrdersClient> _logger;

    public OrdersClient(HttpClient httpClient, ILogger<OrdersClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AppointmentApiResponse?> GetAppointmentByIdAsync(long id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Calling bookings-api for appointment #{AppointmentId}", id);
        using var response = await _httpClient.GetAsync($"/api/appointments/{id}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AppointmentApiResponse>(cancellationToken: cancellationToken);
    }
}
