using Serilog;
using Bookings.Api.Middleware;
using Bookings.Bll.Interfaces;
using Bookings.Bll.Mapping;
using Bookings.Bll.Services;
using Bookings.Dal.Interfaces;
using Bookings.Dal.Repositories;

var builder = WebApplication.CreateBuilder(args);

// 1. Налаштування структурного логування Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// 2. Реєстрація контролерів та документації Swagger/OpenAPI
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

// 3. Глобальна обробка помилок через IExceptionHandler -> RFC 7807 ProblemDetails
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

// 4. Реєстрація AutoMapper (профілі з рівня BLL + LicenseKey з конфігурації)
builder.Services.AddAutoMapper(cfg =>
{
    cfg.LicenseKey = builder.Configuration["AutoMapper:LicenseKey"];
}, typeof(BookingProfile).Assembly);

// 5. Реєстрація DAL (Unit of Work зі спільним з'єднанням на Scoped-запит)
builder.Services.AddScoped<IUnitOfWork>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connectionString = config.GetConnectionString("BookingsDb")
        ?? throw new InvalidOperationException(
            "Рядок підключення 'BookingsDb' не знайдено у конфігурації. Перевизначте через ConnectionStrings__BookingsDb.");
    return new UnitOfWork(connectionString);
});

// 6. Реєстрація сервісів бізнес-логіки (BLL)
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<ICatalogQueryService, CatalogQueryService>();

var app = builder.Build();

// Пайплайн обробки HTTP-запитів
app.UseExceptionHandler();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Bookings API v1");
});

app.UseAuthorization();
app.MapControllers();

app.Run();

// Частковий клас Program для інтеграційного тестування через WebApplicationFactory<Program>
public partial class Program { }
