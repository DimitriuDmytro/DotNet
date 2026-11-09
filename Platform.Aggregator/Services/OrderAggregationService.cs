namespace Platform.Aggregator.Services;

using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Platform.Aggregator.Clients;
using Platform.Aggregator.Dtos;

public class ServiceUnavailableException : Exception
{
    public ServiceUnavailableException(string serviceName, string message, Exception? innerException = null)
        : base($"{serviceName} is unavailable: {message}", innerException)
    {
        ServiceName = serviceName;
    }

    public string ServiceName { get; }
}

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

        // 1. Отримання основної сутності:
        // Якщо повернуто 404 — appointment == null (результат null -> 404 NotFound)
        // Якщо сервіс недоступний / помилка мережі — кидаємо ServiceUnavailableException (результат -> 503)
        AppointmentApiResponse? appointment;
        var sw = Stopwatch.StartNew();
        try
        {
            appointment = await _ordersClient.GetAppointmentByIdAsync(appointmentId, cancellationToken);
            sw.Stop();
            _logger.LogInformation("bookings-api responded in {ElapsedMs} ms", sw.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "bookings-api is unavailable after {ElapsedMs} ms: {Message}", sw.ElapsedMilliseconds, ex.Message);
            throw new ServiceUnavailableException("bookings-api", ex.Message, ex);
        }

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

        // 2. Паралельні незалежні виклики до Catalog та Reviews для кожної позиції замовлення (AG-14, AG-15)
        var lineTasks = items.Select(async item =>
        {
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

            // Перевірка узгодженості (AG-33): отриманий товар з каталогу повинен відповідати замовленій послузі
            if (product != null && product.Id != item.ServiceId)
            {
                warnings.Add($"catalog-api data inconsistency for service #{item.ServiceId}: received #{product.Id}");
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
            warnings.OrderBy(w => w).ToList());
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
            _logger.LogInformation("{Service} responded in {ElapsedMs} ms", serviceName, sw.ElapsedMilliseconds);
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Скасування запиту клієнтом не ковтається (AG-22)
            throw;
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            sw.Stop();
            // Повне логування винятку для внутрішньої діагностики
            _logger.LogWarning(
                ex,
                "{Service} is unavailable after {ElapsedMs} ms: {Message}",
                serviceName,
                sw.ElapsedMilliseconds,
                ex.Message);

            // Попередження для клієнта без розкриття внутрішніх деталей (AG-25)
            warnings.Add($"{serviceName} is unavailable");
            return default;
        }
    }
}
