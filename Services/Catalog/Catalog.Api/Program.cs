using Catalog.Api.Grpc;
using Catalog.Api.Middleware;
using Catalog.Bll.Caching;
using Catalog.Bll.Interfaces;
using Catalog.Bll.Mapping;
using Catalog.Bll.Services;
using Catalog.Bll.Validators;
using Catalog.Dal;
using Catalog.Dal.Interfaces;
using Catalog.Dal.Repositories;
using Catalog.Dal.Seeding;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Platform.ServiceDefaults;
using Platform.ServiceDefaults.Grpc;

var builder = WebApplication.CreateBuilder(args);

// Aspire Service Defaults (Serilog, OpenTelemetry, HealthChecks, ServiceDiscovery, Resilience)
builder.AddServiceDefaults();

// Redis L2 Distributed Cache (Aspire component)
builder.AddRedisDistributedCache("redis");

// L1 Memory Cache з обмеженням розміру (SizeLimit) та відстеженням статистики
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 10_000;
    options.CompactionPercentage = 0.25;
    options.TrackStatistics = true;
});

// Дворівневий кеш (L1 + L2), фоновий прогрів та декоратор сервісів
builder.Services.AddCatalogCaching();

// Health Check кешу (Healthy якщо L1 і Redis живі, Degraded якщо тільки L1)
builder.Services.AddHealthChecks().AddCheck<CacheHealthCheck>("cache", tags: ["ready"]);

// gRPC сервер із сервером-перехоплювачем логування та кореляції
builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<GrpcLoggingInterceptor>();
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});
builder.Services.AddGrpcReflection();

// Реєстрація контролерів та Swagger/OpenAPI
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Detailing Studio — Catalog Microservice API",
        Version = "v1",
        Description = "Тришарова архітектура на базі EF Core Code-First, Ardalis.Specification, Generic Repository та Unit of Work (Лаб. 03)"
    });
});

// RFC 7807 ProblemDetails & DomainExceptionHandler
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

// AutoMapper (профілі BLL + необов'язковий ключ ліцензії)
builder.Services.AddAutoMapper(cfg =>
{
    var licenseKey = builder.Configuration["AutoMapper:LicenseKey"];
    if (!string.IsNullOrWhiteSpace(licenseKey))
    {
        cfg.LicenseKey = licenseKey;
    }
}, typeof(CatalogProfile).Assembly);

// Валідатори FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateServiceDtoValidator>();

// EF Core DbContext
var connectionString = builder.Configuration.GetConnectionString("CatalogDb")
    ?? "Server=(localdb)\\mssqllocaldb;Database=DetailingCatalog;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

builder.Services.AddDbContext<CatalogDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.MigrationsAssembly(typeof(CatalogDbContext).Assembly.FullName);
    });

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Реєстрація DAL (Unit of Work та репозиторіїв)
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IServiceCategoryRepository, ServiceCategoryRepository>();
builder.Services.AddScoped<IServiceRepository, ServiceRepository>();
builder.Services.AddScoped<IPackageRepository, PackageRepository>();

// Реєстрація BLL сервісів (IServiceService реєструється через AddCatalogCaching)
builder.Services.AddScoped<IServiceCategoryService, ServiceCategoryService>();
builder.Services.AddScoped<IPackageService, PackageService>();

var app = builder.Build();

// Middleware кореляційного ID одразу після Build
app.UseMiddleware<CorrelationIdMiddleware>();

// Глобальна обробка помилок
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalog API v1");
    });
}

app.UseAuthorization();
app.MapControllers();
app.MapGrpcService<CatalogGrpcService>();
if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}
app.MapDefaultEndpoints();

// Автоматична міграція та сідінг БД під час запуску (для dev/staging середовищ)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        if (db.Database.IsRelational())
        {
            await db.Database.MigrateAsync();
        }
        if (!app.Environment.IsProduction())
        {
            await DbInitializer.SeedAsync(db);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Помилка автоматичної міграції або сідінгу БД CatalogDb під час запуску");
    }
}

app.Run();

public partial class Program { }
