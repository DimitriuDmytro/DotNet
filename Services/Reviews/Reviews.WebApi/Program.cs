using Microsoft.Extensions.Options;
using Reviews.Application;
using Reviews.Infrastructure;
using Reviews.Infrastructure.Persistence;
using Reviews.Infrastructure.Seeding;
using Reviews.WebApi.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Налаштування Serilog (Критерій 9)
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// Реєстрація шарів Clean Architecture (Критерій 1, 7, 10)
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

// ProblemDetails та централізований обробник помилок (Критерій 13)
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

// Web API контролери та Swagger (Критерій 12)
builder.Services.AddControllers();
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

// Health Checks для MongoDB (Критерій 14)
builder.Services.AddHealthChecks()
    .AddMongoDb(
        sp => sp.GetRequiredService<MongoDbContext>().Database,
        name: "mongodb",
        timeout: TimeSpan.FromSeconds(3));

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Reviews API v1"));
}

app.MapControllers();
app.MapHealthChecks("/health");

// Ідемпотентне наповнення початковими даними при запуску (Критерій 14)
if (!app.Environment.IsEnvironment("Testing"))
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
