using Catalog.Api.Middleware;
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
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// 1. Налаштування структурного логування Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// 2. Реєстрація контролерів та Swagger/OpenAPI
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

// 3. RFC 7807 ProblemDetails & DomainExceptionHandler
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

// 4. AutoMapper (профілі BLL + необов'язковий ключ ліцензії)
builder.Services.AddAutoMapper(cfg =>
{
    var licenseKey = builder.Configuration["AutoMapper:LicenseKey"];
    if (!string.IsNullOrWhiteSpace(licenseKey))
    {
        cfg.LicenseKey = licenseKey;
    }
}, typeof(CatalogProfile).Assembly);

// 5. Валідатори FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateServiceDtoValidator>();

// 6. EF Core DbContext
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

// 7. Реєстрація DAL (Unit of Work та репозиторіїв)
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IServiceCategoryRepository, ServiceCategoryRepository>();
builder.Services.AddScoped<IServiceRepository, ServiceRepository>();
builder.Services.AddScoped<IPackageRepository, PackageRepository>();

// 8. Реєстрація BLL сервісів
builder.Services.AddScoped<IServiceCategoryService, ServiceCategoryService>();
builder.Services.AddScoped<IServiceService, ServiceService>();
builder.Services.AddScoped<IPackageService, PackageService>();

var app = builder.Build();

// 9. Middleware кореляційного ID (додає X-Correlation-Id у заголовок та Serilog LogContext)
app.UseMiddleware<CorrelationIdMiddleware>();

// 10. Глобальна обробка помилок
app.UseExceptionHandler();

// 11. Структурне логування HTTP-запитів Serilog
app.UseSerilogRequestLogging();

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

// 12. Автоматична міграція та сідінг БД під час запуску (для dev/staging середовищ)
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

try
{
    app.Run();
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
