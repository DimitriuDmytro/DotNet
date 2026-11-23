using System.Text.Json;
using System.Text.Json.Serialization;
using Bookings.Grpc;
using Catalog.Grpc;
using Grpc.Core;
using Grpc.Health.V1;
using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Http.Resilience;
using Platform.Aggregator.Clients;
using Platform.Aggregator.Grpc;
using Platform.Aggregator.HealthChecks;
using Platform.Aggregator.Security;
using Platform.Aggregator.Services;
using Platform.ServiceDefaults;
using Polly;
using Reviews.Grpc;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Aspire Service Defaults (Serilog, OpenTelemetry, HealthChecks, ServiceDiscovery, StandardResilience)
builder.AddServiceDefaults();

// Redis L2 Distributed Cache для кешування агрегованих відповідей
builder.AddRedisDistributedCache("redis");

// Реєстрація Health Check для Redis зі статусом Degraded (Критерій 9)
var redisConnectionString = builder.Configuration.GetConnectionString("redis") ?? "localhost:6379";
builder.Services.AddHealthChecks()
    .AddRedis(
        redisConnectionString,
        name: "redis-cache",
        failureStatus: HealthStatus.Degraded,
        tags: ["cache", "redis", "ready"],
        timeout: TimeSpan.FromSeconds(3));

// HttpContextAccessor для отримання кореляційного ID в gRPC інтерцепторі
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<GrpcClientLoggingInterceptor>();
builder.Services.AddSingleton<AggregatorCacheMetrics>();

// IdentityServer M2M Token Service & Auth Handler (К9)
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<IIdentityServerTokenService, IdentityServerTokenService>();
builder.Services.AddTransient<IdentityServerAuthHandler>();

// ProblemDetails для стандартизованих помилок (AG-27)
builder.Services.AddProblemDetails();

// JSON серіалізація: camelCase та JsonIgnoreCondition.WhenWritingNull (AG-34)
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Критерій 2: gRPC Retry Policy через ServiceConfig каналу
// Конфігурація ретраїв для критичних сервісів (Bookings, Catalog): 4 спроби, exponential backoff (0.5s - 4s)
var criticalGrpcMethodConfig = new MethodConfig
{
    Names = { MethodName.Default },
    RetryPolicy = new RetryPolicy
    {
        MaxAttempts = 4,
        InitialBackoff = TimeSpan.FromMilliseconds(500),
        MaxBackoff = TimeSpan.FromSeconds(4),
        BackoffMultiplier = 2,
        RetryableStatusCodes = { StatusCode.Unavailable, StatusCode.ResourceExhausted }
    }
};

// Конфігурація ретраїв для некритичного сервісу (Reviews): 2 спроби, швидкий вихід для уникнення затримок
var nonCriticalGrpcMethodConfig = new MethodConfig
{
    Names = { MethodName.Default },
    RetryPolicy = new RetryPolicy
    {
        MaxAttempts = 2,
        InitialBackoff = TimeSpan.FromMilliseconds(200),
        MaxBackoff = TimeSpan.FromSeconds(1),
        BackoffMultiplier = 1.5,
        RetryableStatusCodes = { StatusCode.Unavailable }
    }
};

// Реєстрація gRPC-клієнтів до мікросервісів з політиками повторів та інтерцептором логування
builder.Services.AddGrpcClient<BookingsService.BookingsServiceClient>(c =>
{
    c.Address = new Uri("https://bookings-api");
})
.ConfigureChannel(options =>
{
    options.ServiceConfig = new ServiceConfig
    {
        MethodConfigs = { criticalGrpcMethodConfig }
    };
})
.AddInterceptor<GrpcClientLoggingInterceptor>();

builder.Services.AddGrpcClient<CatalogService.CatalogServiceClient>(c =>
{
    c.Address = new Uri("https://catalog-api");
})
.ConfigureChannel(options =>
{
    options.ServiceConfig = new ServiceConfig
    {
        MethodConfigs = { criticalGrpcMethodConfig }
    };
})
.AddInterceptor<GrpcClientLoggingInterceptor>();

builder.Services.AddGrpcClient<ReviewsService.ReviewsServiceClient>(c =>
{
    c.Address = new Uri("https://reviews-api");
})
.ConfigureChannel(options =>
{
    options.ServiceConfig = new ServiceConfig
    {
        MethodConfigs = { nonCriticalGrpcMethodConfig }
    };
})
.AddInterceptor<GrpcClientLoggingInterceptor>();

// Критерій 10: gRPC Health Checking Protocol клієнти та перевірки здоров'я downstream сервісів
builder.Services.AddGrpcClient<Health.HealthClient>("BookingsHealth", c => c.Address = new Uri("https://bookings-api"));
builder.Services.AddGrpcClient<Health.HealthClient>("CatalogHealth", c => c.Address = new Uri("https://catalog-api"));
builder.Services.AddGrpcClient<Health.HealthClient>("ReviewsHealth", c => c.Address = new Uri("https://reviews-api"));

builder.Services.AddHealthChecks()
    .AddCheck<BookingsGrpcHealthCheck>("bookings-api-grpc", tags: ["grpc", "downstream", "ready"])
    .AddCheck<CatalogGrpcHealthCheck>("catalog-api-grpc", tags: ["grpc", "downstream", "ready"])
    .AddCheck<ReviewsGrpcHealthCheck>("reviews-api-grpc", tags: ["grpc", "downstream", "ready"]);

// Критерії 3 та 4: Кастомні конвеєри стійкості HTTP-клієнтів з хуками логування та диференційованими політиками

// 1. CatalogClient (Критичний сервіс: читання деталей послуг)
builder.Services.AddHttpClient<CatalogClient>(c => c.BaseAddress = new("https+http://catalog-api"))
    .AddHttpMessageHandler<IdentityServerAuthHandler>()
    .RemoveAllResilienceHandlers()
    .AddStandardResilienceHandler(options =>
    {
        options.Retry.MaxRetryAttempts = 5;
        options.Retry.BackoffType = DelayBackoffType.Exponential;
        options.Retry.UseJitter = true;
        options.Retry.Delay = TimeSpan.FromMilliseconds(500);
        options.Retry.DisableForUnsafeHttpMethods();
        options.Retry.OnRetry = args =>
        {
            Log.Warning("Resilience: Retry attempt #{Attempt} for CatalogClient after {DelayMs}ms. Reason: {Outcome}",
                args.AttemptNumber, args.RetryDelay.TotalMilliseconds, args.Outcome.Exception?.Message ?? args.Outcome.Result?.StatusCode.ToString());
            return ValueTask.CompletedTask;
        };

        options.CircuitBreaker.FailureRatio = 0.3;
        options.CircuitBreaker.MinimumThroughput = 10;
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);
        options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
        options.CircuitBreaker.OnOpened = args =>
        {
            Log.Error("Resilience: Circuit Breaker OPENED for CatalogClient. Break duration: {DurationMs}ms",
                args.BreakDuration.TotalMilliseconds);
            return ValueTask.CompletedTask;
        };
        options.CircuitBreaker.OnClosed = args =>
        {
            Log.Information("Resilience: Circuit Breaker CLOSED for CatalogClient. Normal operations resumed.");
            return ValueTask.CompletedTask;
        };
        options.CircuitBreaker.OnHalfOpened = args =>
        {
            Log.Warning("Resilience: Circuit Breaker HALF-OPENED for CatalogClient. Testing service health...");
            return ValueTask.CompletedTask;
        };

        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(3);
        options.AttemptTimeout.OnTimeout = args =>
        {
            Log.Warning("Resilience: Attempt timeout for CatalogClient after {TimeoutMs}ms",
                args.Timeout.TotalMilliseconds);
            return ValueTask.CompletedTask;
        };

        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(20);
    });

// 2. OrdersClient (Критичний сервіс: бронювання та замовлення)
builder.Services.AddHttpClient<OrdersClient>(c => c.BaseAddress = new("https+http://bookings-api"))
    .AddHttpMessageHandler<IdentityServerAuthHandler>()
    .RemoveAllResilienceHandlers()
    .AddStandardResilienceHandler(options =>
    {
        options.Retry.MaxRetryAttempts = 3;
        options.Retry.BackoffType = DelayBackoffType.Exponential;
        options.Retry.UseJitter = true;
        options.Retry.Delay = TimeSpan.FromMilliseconds(500);
        options.Retry.DisableForUnsafeHttpMethods();
        options.Retry.OnRetry = args =>
        {
            Log.Warning("Resilience: Retry attempt #{Attempt} for OrdersClient after {DelayMs}ms",
                args.AttemptNumber, args.RetryDelay.TotalMilliseconds);
            return ValueTask.CompletedTask;
        };

        options.CircuitBreaker.FailureRatio = 0.4;
        options.CircuitBreaker.MinimumThroughput = 5;
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);
        options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
        options.CircuitBreaker.OnOpened = args =>
        {
            Log.Error("Resilience: Circuit Breaker OPENED for OrdersClient. Break duration: {DurationMs}ms",
                args.BreakDuration.TotalMilliseconds);
            return ValueTask.CompletedTask;
        };
        options.CircuitBreaker.OnClosed = args =>
        {
            Log.Information("Resilience: Circuit Breaker CLOSED for OrdersClient.");
            return ValueTask.CompletedTask;
        };
        options.CircuitBreaker.OnHalfOpened = args =>
        {
            Log.Warning("Resilience: Circuit Breaker HALF-OPENED for OrdersClient.");
            return ValueTask.CompletedTask;
        };

        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(3);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(15);
    });

// 3. ReviewsClient (Некритичний сервіс: швидкий fallback при недоступності)
builder.Services.AddHttpClient<ReviewsClient>(c => c.BaseAddress = new("https+http://reviews-api"))
    .RemoveAllResilienceHandlers()
    .AddStandardResilienceHandler(options =>
    {
        options.Retry.MaxRetryAttempts = 1;
        options.Retry.Delay = TimeSpan.FromMilliseconds(200);
        options.Retry.OnRetry = args =>
        {
            Log.Warning("Resilience: Retry attempt #{Attempt} for ReviewsClient", args.AttemptNumber);
            return ValueTask.CompletedTask;
        };

        options.CircuitBreaker.FailureRatio = 0.7;
        options.CircuitBreaker.MinimumThroughput = 5;
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);
        options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(5);
        options.CircuitBreaker.OnOpened = args =>
        {
            Log.Warning("Resilience: Circuit Breaker OPENED for ReviewsClient. Fast partial degradation active.");
            return ValueTask.CompletedTask;
        };
        options.CircuitBreaker.OnClosed = args =>
        {
            Log.Information("Resilience: Circuit Breaker CLOSED for ReviewsClient.");
            return ValueTask.CompletedTask;
        };
        options.CircuitBreaker.OnHalfOpened = args =>
        {
            Log.Information("Resilience: Circuit Breaker HALF-OPENED for ReviewsClient.");
            return ValueTask.CompletedTask;
        };

        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(2);
        options.AttemptTimeout.OnTimeout = args =>
        {
            Log.Warning("Resilience: Attempt timeout for ReviewsClient after {TimeoutMs}ms",
                args.Timeout.TotalMilliseconds);
            return ValueTask.CompletedTask;
        };

        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(5);
    });

// Сервіси бізнес-агрегації
builder.Services.AddScoped<OrderAggregationService>();
builder.Services.AddScoped<IOrderAggregationService, GrpcOrderAggregationService>();

var app = builder.Build();

// CorrelationId Middleware
app.UseMiddleware<CorrelationIdMiddleware>();

// Обробка винятків у ProblemDetails
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
app.MapDefaultEndpoints();

app.Run();
