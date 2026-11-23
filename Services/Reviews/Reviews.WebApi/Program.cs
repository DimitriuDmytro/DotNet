using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Platform.ServiceDefaults;
using Platform.ServiceDefaults.Grpc;
using Reviews.Application;
using Reviews.Application.Caching;
using Reviews.Infrastructure;
using Reviews.Infrastructure.Persistence;
using Reviews.Infrastructure.Seeding;
using Reviews.WebApi.Grpc;
using Reviews.WebApi.Metrics;
using Reviews.WebApi.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Aspire Service Defaults (Serilog, OpenTelemetry, HealthChecks, ServiceDiscovery, Resilience)
builder.AddServiceDefaults();

// Redis L2 Distributed Cache
builder.AddRedisDistributedCache("redis");

// RabbitMQ Messaging via Aspire (Критерій 2, 7)
builder.AddRabbitMQClient("messaging");
builder.Services.AddHostedService<Reviews.WebApi.IntegrationEvents.CatalogEventsConsumer>();
builder.Services.AddScoped<Platform.EventBus.Consumer.IIntegrationEventHandler<Platform.EventBus.Events.ProductRenamedEvent>, Reviews.WebApi.IntegrationEvents.Handlers.ProductRenamedEventHandler>();
builder.Services.AddScoped<Platform.EventBus.Consumer.IIntegrationEventHandler<Platform.EventBus.Events.ProductPriceChangedEvent>, Reviews.WebApi.IntegrationEvents.Handlers.ProductPriceChangedEventHandler>();
builder.Services.AddScoped<Platform.EventBus.Consumer.IIntegrationEventHandler<Platform.EventBus.Events.ProductCreatedEvent>, Reviews.WebApi.IntegrationEvents.Handlers.ProductCreatedEventHandler>();
builder.Services.AddScoped<Platform.EventBus.Consumer.IIntegrationEventHandler<Platform.EventBus.Events.ProductDeletedEvent>, Reviews.WebApi.IntegrationEvents.Handlers.ProductDeletedEventHandler>();

// L1 Memory Cache з обмеженням розміру (SizeLimit) та відстеженням статистики
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 10_000;
    options.CompactionPercentage = 0.25;
    options.TrackStatistics = true;
});

// Реєстрація шарів Clean Architecture (Критерій 1, 7, 10 + MediatR кешування)
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

// Бізнес-метрики мікросервісу (Критерії 6, 7)
builder.Services.AddSingleton<ReviewMetrics>();

// Health Checks для MongoDB та Redis кешу зі статусом Degraded (Критерії 8, 9)
var redisConnectionString = builder.Configuration.GetConnectionString("redis") ?? "localhost:6379";
builder.Services.AddHealthChecks()
    .AddMongoDb(
        sp => sp.GetRequiredService<MongoDbContext>().Database,
        name: "reviews-db",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["database", "mongo", "ready"],
        timeout: TimeSpan.FromSeconds(5))
    .AddRedis(
        redisConnectionString,
        name: "redis-cache",
        failureStatus: HealthStatus.Degraded,
        tags: ["cache", "redis", "ready"],
        timeout: TimeSpan.FromSeconds(3))
    .AddCheck<ReviewsCacheHealthCheck>("cache", tags: ["ready"]);

// gRPC сервер із перехоплювачем логування та підтримкою gRPC Health Checking Protocol (Критерій 10)
builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<GrpcLoggingInterceptor>();
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});
builder.Services.AddGrpcReflection();
builder.Services.AddGrpcHealthChecks();

// ProblemDetails та централізований обробник помилок (Критерій 13)
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

// Web API контролери та Swagger (Критерій 12)
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Валідація виконується через MediatR ValidationBehavior та FluentValidation (Критерій 8)
        options.SuppressModelStateInvalidFilter = true;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Detailing Reviews API (Lab 04)",
        Version = "v1",
        Description = "Мікросервіс відгуків детейлінг-студії на Clean Architecture + CQRS + MediatR + MongoDB."
    });
});

var app = builder.Build();

// CorrelationId middleware одразу після Build
app.UseMiddleware<CorrelationIdMiddleware>();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Reviews API v1"));
}

app.MapControllers();
app.MapGrpcService<ReviewsGrpcService>();
app.MapGrpcHealthChecksService();
if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}
app.MapDefaultEndpoints();

// Ідемпотентне наповнення початковими даними при запуску (Критерій 14)
var mongoOptions = app.Services.GetRequiredService<IOptions<MongoDbOptions>>().Value;
if (!app.Environment.IsEnvironment("Testing") && mongoOptions.RunStartupTasks)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetService<IDataSeeder>();
        if (seeder != null)
        {
            await seeder.SeedAsync();
        }
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Помилка запуску початкового сідінгу MongoDB.");
    }
}

app.Run();

public partial class Program { }
