namespace Platform.Aggregator.Services;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Bookings.Grpc;
using Catalog.Grpc;
using global::Grpc.Core;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Platform.Aggregator.Dtos;
using Reviews.Grpc;

public sealed class GrpcOrderAggregationService : IOrderAggregationService
{
    private static readonly ActivitySource Source = new("Platform.Aggregator");
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const decimal NanoFactor = 1_000_000_000m;

    private readonly BookingsService.BookingsServiceClient _bookingsClient;
    private readonly CatalogService.CatalogServiceClient _catalogClient;
    private readonly ReviewsService.ReviewsServiceClient _reviewsClient;
    private readonly IDistributedCache _distributedCache;
    private readonly AggregatorCacheMetrics _metrics;
    private readonly OrderAggregationService _httpService;
    private readonly ILogger<GrpcOrderAggregationService> _logger;

    public GrpcOrderAggregationService(
        BookingsService.BookingsServiceClient bookingsClient,
        CatalogService.CatalogServiceClient catalogClient,
        ReviewsService.ReviewsServiceClient reviewsClient,
        IDistributedCache distributedCache,
        AggregatorCacheMetrics metrics,
        OrderAggregationService httpService,
        ILogger<GrpcOrderAggregationService> logger)
    {
        _bookingsClient = bookingsClient;
        _catalogClient = catalogClient;
        _reviewsClient = reviewsClient;
        _distributedCache = distributedCache;
        _metrics = metrics;
        _httpService = httpService;
        _logger = logger;
    }

    public async Task<FullOrderDto?> GetFullOrderAsync(long appointmentId, bool noCache, CancellationToken cancellationToken)
    {
        using var activity = Source.StartActivity("AggregateFullOrder_gRPC");
        activity?.SetTag("appointment.id", appointmentId);
        activity?.SetTag("aggregator.protocol", "gRPC");

        var cacheKey = $"aggregator:order:{appointmentId}:full";

        // 1. Перевірка кешу агрегатора в Redis (якщо не передано noCache=true)
        if (!noCache)
        {
            try
            {
                var cachedBytes = await _distributedCache.GetAsync(cacheKey, cancellationToken);
                if (cachedBytes is not null && cachedBytes.Length > 0)
                {
                    var cachedDto = JsonSerializer.Deserialize<FullOrderDto>(cachedBytes, JsonOptions);
                    if (cachedDto is not null)
                    {
                        _metrics.Hit();
                        _logger.LogInformation("Cache hit for aggregated order #{AppointmentId}", appointmentId);
                        activity?.SetTag("cache.hit", true);
                        return cachedDto;
                    }
                }
                _metrics.Miss();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Помилка читання з Redis кешу агрегатора для #{AppointmentId}", appointmentId);
            }
        }
        else
        {
            _logger.LogInformation("Кеш агрегатора оминається (noCache=true) для замовлення #{AppointmentId}", appointmentId);
        }

        var warnings = new ConcurrentBag<string>();

        // 2. Отримання запису через Bookings gRPC
        AppointmentReply? appointment;
        var sw = Stopwatch.StartNew();
        try
        {
            var request = new GetAppointmentRequest { Id = appointmentId };
            appointment = await _bookingsClient.GetAppointmentAsync(request, cancellationToken: cancellationToken);
            sw.Stop();
            _logger.LogInformation("bookings-api gRPC responded in {ElapsedMs} ms", sw.ElapsedMilliseconds);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            sw.Stop();
            _logger.LogInformation("Замовлення #{AppointmentId} не знайдено в bookings-api (NotFound)", appointmentId);
            return null;
        }
        catch (RpcException ex) when (ex.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded)
        {
            sw.Stop();
            _logger.LogWarning(ex, "bookings-api gRPC недоступний: {Status}", ex.Status);
            throw new ServiceUnavailableException("bookings-api", ex.Status.Detail, ex);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            sw.Stop();
            _logger.LogWarning(ex, "bookings-api gRPC помилка виклику");
            throw new ServiceUnavailableException("bookings-api", ex.Message, ex);
        }

        var orderSummary = new OrderSummaryDto(
            appointment.Id,
            appointment.ClientId,
            appointment.ClientFullName,
            appointment.ScheduledAt.ToDateTime(),
            appointment.Status,
            $"{appointment.CarBrand} {appointment.CarModel}".Trim(),
            ToDecimal(appointment.TotalAmount));

        // 3. Паралельні незалежні gRPC виклики до Catalog та Reviews для кожної позиції замовлення
        var items = appointment.Items;
        var lineTasks = items.Select(async item =>
        {
            var productTask = SafeExecuteAsync(
                "catalog-api",
                async () =>
                {
                    var req = new GetServiceRequest { Id = item.ServiceId };
                    return await _catalogClient.GetServiceAsync(req, cancellationToken: cancellationToken);
                },
                warnings,
                cancellationToken);

            var reviewsTask = SafeExecuteAsync(
                "reviews-api",
                async () =>
                {
                    var req = new GetReviewsForServiceRequest
                    {
                        ServiceId = item.ServiceId,
                        Page = 1,
                        PageSize = 20
                    };
                    return await _reviewsClient.GetReviewsForServiceAsync(req, cancellationToken: cancellationToken);
                },
                warnings,
                cancellationToken);

            await Task.WhenAll(productTask, reviewsTask);

            var catalogReply = await productTask;
            var reviewsReply = await reviewsTask;

            ProductDto? product = null;
            if (catalogReply is not null)
            {
                if (catalogReply.Id != item.ServiceId)
                {
                    warnings.Add($"catalog-api data inconsistency for service #{item.ServiceId}: received #{catalogReply.Id}");
                }
                else
                {
                    product = new ProductDto(
                        catalogReply.Id,
                        catalogReply.CategoryId,
                        catalogReply.Name,
                        ToDecimal(catalogReply.Price),
                        catalogReply.DurationMinutes);
                }
            }

            var reviews = new List<ReviewDto>();
            if (reviewsReply?.Reviews is not null)
            {
                foreach (var r in reviewsReply.Reviews)
                {
                    reviews.Add(new ReviewDto(
                        r.Id,
                        r.ServiceId,
                        r.ServiceName,
                        r.Rating,
                        r.Title,
                        r.Text,
                        r.AuthorName,
                        r.CreatedAt.ToDateTime()));
                }
            }

            return new OrderLineDto(
                item.ServiceId,
                item.ServiceName,
                ToDecimal(item.UnitPrice),
                item.Quantity,
                ToDecimal(item.Discount),
                product,
                reviews);
        });

        var lines = await Task.WhenAll(lineTasks);

        var fullOrder = new FullOrderDto(
            orderSummary,
            lines.ToList(),
            warnings.OrderBy(w => w).ToList());

        // 4. Кешування агрегованої картки в Redis:
        // УВАГА: частково неуспішні відповіді (з warnings) НЕ кешуємо!
        if (!noCache && warnings.IsEmpty)
        {
            try
            {
                var bytes = JsonSerializer.SerializeToUtf8Bytes(fullOrder, JsonOptions);
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60)
                };
                await _distributedCache.SetAsync(cacheKey, bytes, options, cancellationToken);
                _logger.LogInformation("Cached full aggregated order #{AppointmentId} in Redis for 60s", appointmentId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cache aggregated order #{AppointmentId} in Redis", appointmentId);
            }
        }
        else if (!warnings.IsEmpty)
        {
            _logger.LogInformation("Агреговану картку #{AppointmentId} не закешовано через наявність попереджень ({Count})",
                appointmentId, warnings.Count);
        }

        return fullOrder;
    }

    public async Task<FullOrderDto?> GetFullOrderHttpAsync(long appointmentId, bool noCache, CancellationToken cancellationToken)
    {
        // HTTP-версія для порівняльного бенчмарку
        return await _httpService.GetFullOrderAsync(appointmentId, cancellationToken);
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
            _logger.LogInformation("{Service} gRPC responded in {ElapsedMs} ms", serviceName, sw.ElapsedMilliseconds);
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            sw.Stop();
            _logger.LogInformation("{Service} item not found (NotFound)", serviceName);
            return default;
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            sw.Stop();
            _logger.LogWarning(ex, "{Service} gRPC unavailable after {ElapsedMs} ms: {Message}",
                serviceName, sw.ElapsedMilliseconds, ex.Message);
            warnings.Add($"{serviceName} is unavailable");
            return default;
        }
    }

    private static decimal ToDecimal(Bookings.Grpc.DecimalValue val)
    {
        if (val is null) return 0m;
        return val.Units + (val.Nanos / NanoFactor);
    }

    private static decimal ToDecimal(Catalog.Grpc.DecimalValue val)
    {
        if (val is null) return 0m;
        return val.Units + (val.Nanos / NanoFactor);
    }
}
