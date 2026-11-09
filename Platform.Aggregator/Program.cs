using System.Text.Json;
using System.Text.Json.Serialization;
using Platform.Aggregator.Clients;
using Platform.Aggregator.Services;
using Platform.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// Aspire Service Defaults
builder.AddServiceDefaults();

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

// Реєстрація типізованих HTTP-клієнтів до мікросервісів за логічними іменами через Aspire Service Discovery (AG-01, AG-03)
builder.Services.AddHttpClient<OrdersClient>(c => c.BaseAddress = new("https+http://bookings-api"));
builder.Services.AddHttpClient<CatalogClient>(c => c.BaseAddress = new("https+http://catalog-api"));
builder.Services.AddHttpClient<ReviewsClient>(c => c.BaseAddress = new("https+http://reviews-api"));

// Сервіс бізнес-агрегації
builder.Services.AddScoped<OrderAggregationService>();

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
