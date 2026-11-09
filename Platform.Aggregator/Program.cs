using System.Text.Json;
using System.Text.Json.Serialization;
using Bookings.Grpc;
using Catalog.Grpc;
using Platform.Aggregator.Clients;
using Platform.Aggregator.Grpc;
using Platform.Aggregator.Services;
using Platform.ServiceDefaults;
using Reviews.Grpc;

var builder = WebApplication.CreateBuilder(args);

// Aspire Service Defaults
builder.AddServiceDefaults();

// Redis L2 Distributed Cache для кешування агрегованих відповідей
builder.AddRedisDistributedCache("redis");

// HttpContextAccessor для отримання кореляційного ID в gRPC інтерцепторі
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<GrpcClientLoggingInterceptor>();
builder.Services.AddSingleton<AggregatorCacheMetrics>();

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

// Реєстрація gRPC-клієнтів до мікросервісів з перехоплювачем логування та кореляції
builder.Services.AddGrpcClient<BookingsService.BookingsServiceClient>(c =>
{
    c.Address = new Uri("https://bookings-api");
}).AddInterceptor<GrpcClientLoggingInterceptor>();

builder.Services.AddGrpcClient<CatalogService.CatalogServiceClient>(c =>
{
    c.Address = new Uri("https://catalog-api");
}).AddInterceptor<GrpcClientLoggingInterceptor>();

builder.Services.AddGrpcClient<ReviewsService.ReviewsServiceClient>(c =>
{
    c.Address = new Uri("https://reviews-api");
}).AddInterceptor<GrpcClientLoggingInterceptor>();

// Реєстрація типізованих HTTP-клієнтів (для порівняльного бенчмарку /full-http)
builder.Services.AddHttpClient<OrdersClient>(c => c.BaseAddress = new("https+http://bookings-api"));
builder.Services.AddHttpClient<CatalogClient>(c => c.BaseAddress = new("https+http://catalog-api"));
builder.Services.AddHttpClient<ReviewsClient>(c => c.BaseAddress = new("https+http://reviews-api"));

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
