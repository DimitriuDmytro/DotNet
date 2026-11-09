using Bookings.Api.Grpc;
using Bookings.Api.Middleware;
using Bookings.Bll.Caching;
using Bookings.Bll.Interfaces;
using Bookings.Bll.Mapping;
using Bookings.Bll.Services;
using Bookings.Dal.Interfaces;
using Bookings.Dal.Repositories;
using Platform.ServiceDefaults;
using Platform.ServiceDefaults.Grpc;

var builder = WebApplication.CreateBuilder(args);

// Aspire Service Defaults (Serilog, OpenTelemetry, HealthChecks, ServiceDiscovery, Resilience)
builder.AddServiceDefaults();

// Redis L2 Distributed Cache
builder.AddRedisDistributedCache("redis");

// L1 Memory Cache з обмеженням розміру (SizeLimit) та відстеженням статистики
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 10_000;
    options.CompactionPercentage = 0.25;
    options.TrackStatistics = true;
});

// Дворівневий кеш бронювань (L1 + L2) та декоратор сервісів
builder.Services.AddBookingsCaching();

// Health Check кешу
builder.Services.AddHealthChecks().AddCheck<BookingsCacheHealthCheck>("cache", tags: ["ready"]);

// gRPC сервер із сервером-перехоплювачем логування та кореляції
builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<GrpcLoggingInterceptor>();
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

// Реєстрація контролерів та документації Swagger/OpenAPI
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Detailing Studio — Bookings Microservice API",
        Version = "v1",
        Description = "Тришарова архітектура на базі ADO.NET, Dapper, Generic Repository та Unit of Work (Лаб. 02)"
    });
});

// Глобальна обробка помилок через IExceptionHandler -> RFC 7807 ProblemDetails
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

// Реєстрація AutoMapper (профілі з рівня BLL + LicenseKey з конфігурації)
builder.Services.AddAutoMapper(cfg =>
{
    cfg.LicenseKey = builder.Configuration["AutoMapper:LicenseKey"];
}, typeof(BookingProfile).Assembly);

// Реєстрація DAL (Unit of Work зі спільним з'єднанням на Scoped-запит)
builder.Services.AddScoped<IUnitOfWork>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connectionString = config.GetConnectionString("BookingsDb")
        ?? throw new InvalidOperationException(
            "Рядок підключення 'BookingsDb' не знайдено у конфігурації. Перевизначте через ConnectionStrings__BookingsDb.");
    return new UnitOfWork(connectionString);
});

// Реєстрація сервісів бізнес-логіки (BLL)
// (IAppointmentService реєструється через AddBookingsCaching як CachedAppointmentService)
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<ICatalogQueryService, CatalogQueryService>();

var app = builder.Build();

// CorrelationId middleware одразу після Build
app.UseMiddleware<CorrelationIdMiddleware>();

// Пайплайн обробки HTTP-запитів
app.UseExceptionHandler();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Bookings API v1");
});

app.UseAuthorization();
app.MapControllers();
app.MapGrpcService<BookingsGrpcService>();
app.MapDefaultEndpoints();

app.Run();

// Частковий клас Program для інтеграційного тестування через WebApplicationFactory<Program>
public partial class Program { }
