namespace Platform.Aggregator.Services;

using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Platform.Aggregator.Clients;
using Platform.Aggregator.Dtos;

public class OrderAggregationService
{
    private static readonly ActivitySource Source = new("Platform.Aggregator");
    private readonly OrdersClient _ordersClient;
    private readonly CatalogClient _catalogClient;
    private readonly ReviewsClient _reviewsClient;
    private readonly ILogger<OrderAggregationService> _logger;

    public OrderAggregationService(
        OrdersClient ordersClient,
        CatalogClient catalogClient,
        ReviewsClient reviewsClient,
        ILogger<OrderAggregationService> logger)
    {
        _ordersClient = ordersClient;
        _catalogClient = catalogClient;
        _reviewsClient = reviewsClient;
        _logger = logger;
    }

    public async Task<FullOrderDto?> GetFullOrderAsync(long appointmentId, CancellationToken cancellationToken)
    {
        using var activity = Source.StartActivity("AggregateFullOrder");
        activity?.SetTag("appointment.id", appointmentId);

        var warnings = new ConcurrentBag<string>();

        // 1. Отримання основної сутності — без неї 404
        var appointment = await SafeExecuteAsync(
            "bookings-api",
            () => _ordersClient.GetAppointmentByIdAsync(appointmentId, cancellationToken),
            warnings,
            cancellationToken);

        if (appointment == null)
        {
            return null;
        }

        var orderSummary = new OrderSummaryDto(
            appointment.Id,
            appointment.ClientId,
            appointment.ClientFullName ?? "Невідомий клієнт",
            appointment.ScheduledAt,
            appointment.Status,
            $"{appointment.CarBrand} {appointment.CarModel}".Trim(),
            appointment.TotalAmount);

        var items = appointment.Items ?? new List<AppointmentItemApiResponse>();

        // 2. Паралельні незалежні виклики до Catalog та Reviews для кожної позиції замовлення
        var lineTasks = items.Select(async item =>
        {
            // Перевірка узгодженості: serviceId > 0
            var productTask = SafeExecuteAsync(
                "catalog-api",
                () => _catalogClient.GetProductByIdAsync(item.ServiceId, cancellationToken),
                warnings,
                cancellationToken);

            var reviewsTask = SafeExecuteAsync(
                "reviews-api",
                async () =>
                {
                    var revs = await _reviewsClient.GetReviewsByServiceIdAsync(item.ServiceId, cancellationToken);
                    return (IReadOnlyList<ReviewDto>?)revs;
                },
                warnings,
                cancellationToken);

            await Task.WhenAll(productTask, reviewsTask);

            var product = await productTask;
            var reviews = await reviewsTask ?? Array.Empty<ReviewDto>();

            // Перевірка узгодженості: отриманий товар з каталогу повинен мати той самий id
            if (product != null && product.Id != item.ServiceId)
            {
                warnings.Add($"Невідповідність даних каталогу для послуги #{item.ServiceId}: отримано #{product.Id}");
                product = null;
            }

            return new OrderLineDto(
                item.ServiceId,
                item.ServiceName,
                item.UnitPrice,
                item.Quantity,
                item.Discount,
                product,
                reviews);
        });

        var lines = await Task.WhenAll(lineTasks);

        return new FullOrderDto(
            orderSummary,
            lines.ToList(),
            warnings.ToList());
    }

    private async Task<T?> SafeExecuteAsync<T>(
        string serviceName,
        Func<Task<T?>> action,
        ConcurrentBag<string> warnings,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await action();
            sw.Stop();
            _logger.LogInformation("{Service} responded in {Elapsed} ms", serviceName, sw.ElapsedMilliseconds);
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Запит було скасовано клієнтом — прокидаємо далі
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(
                ex,
                "Помилка виклику сервісу '{Service}' після {Elapsed} ms: {Message}",
                serviceName,
                sw.ElapsedMilliseconds,
                ex.Message);

            warnings.Add($"Сервіс '{serviceName}' тимчасово недоступний або повернув помилку: {ex.Message}");
            return default;
        }
    }
}
