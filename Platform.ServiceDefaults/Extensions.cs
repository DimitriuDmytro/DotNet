using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Platform.ServiceDefaults;
using Platform.ServiceDefaults.Grpc;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Розширення для конфігурації загальних сервісів платформи Aspire:
/// Serilog, OpenTelemetry, Service Discovery, Health Checks, Resilience, CorrelationId та gRPC.
/// </summary>
public static class Extensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";

    /// <summary>
    /// Реєструє спільні інфраструктурні сервіси для мікросервісів та шлюзів платформи.
    /// </summary>
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        // Очищаємо стандартні MEL-провайдери, щоб уникнути подвійного виводу в консоль (SD-13)
        builder.Logging.ClearProviders();

        // 4.1 Налаштування централізованого Serilog логера
        builder.Services.AddSerilog((services, cfg) => cfg
            .MinimumLevel.Is(builder.Environment.IsDevelopment() ? LogEventLevel.Debug : LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("ServiceName", builder.Environment.ApplicationName)
            .Enrich.WithEnvironmentName()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .WriteTo.Console(new CompactJsonFormatter()),
            writeToProviders: true);

        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddTransient<CorrelationIdHandler>();
        builder.Services.AddSingleton<GrpcLoggingInterceptor>();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Критерій 1: Захист від каскадних збоїв (Cascading Failures)
            // Стандартний конвеєр відмовостійкості AddStandardResilienceHandler налаштовує:
            // 1) Total Request Timeout (загальний час виконання запиту)
            // 2) Retry Strategy (експоненційний повтор запитів при транзитних помилках)
            // 3) Circuit Breaker (запобіжник для відсікання навантаження від сервісів, що впали)
            // 4) Attempt Timeout (таймаут на окрему спробу)
            // 5) Rate Limiter (обмеження інтенсивності вихідних викликів)
            // Це забезпечує базовий рівень надійності для всіх вихідних HTTP-клієнтів між сервісами платформи.
            http.AddStandardResilienceHandler();

            // Автоматичне виявлення сервісів через Aspire Service Discovery
            http.AddServiceDiscovery();

            // Наскрізне прокидання заголовка X-Correlation-Id між сервісами
            http.AddHttpMessageHandler<CorrelationIdHandler>();
        });

        return builder;
    }

    /// <summary>
    /// Конфігурація OpenTelemetry Metrics та Distributed Tracing.
    /// </summary>
    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: builder.Environment.ApplicationName)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = builder.Environment.EnvironmentName
                }))
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter(
                        "Polly",
                        "Bookings.Api",
                        "Catalog.Api",
                        "Reviews.WebApi",
                        "Catalog.Cache",
                        "Bookings.Cache",
                        "Reviews.Cache",
                        "Aggregator.Cache");
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName)
                    .AddSource("Bookings.Bll")
                    .AddSource("Bookings.Api")
                    .AddSource("Bookings.Cache")
                    .AddSource("Catalog.Bll")
                    .AddSource("Catalog.Api")
                    .AddSource("Catalog.Cache")
                    .AddSource("Reviews.Application")
                    .AddSource("Reviews.WebApi")
                    .AddSource("Reviews.Cache")
                    .AddSource("Platform.Aggregator")
                    .AddSource("Platform.Aggregator.Cache")
                    .AddSource("Platform.ApiGateway")
                    .AddSource("Platform.Messaging")
                    .AddSource("MongoDB.Driver.Core.Extensions.DiagnosticSources")
                    .AddAspNetCoreInstrumentation(opt =>
                    {
                        opt.Filter = context =>
                            !context.Request.Path.StartsWithSegments(HealthEndpointPath)
                            && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath);
                    })
                    .AddHttpClientInstrumentation()
                    .AddSqlClientInstrumentation();

                if (builder.Environment.IsDevelopment())
                {
                    tracing.SetSampler(new AlwaysOnSampler());
                }
                else
                {
                    var samplingRatio = builder.Configuration.GetValue<double?>("OpenTelemetry:SamplingRatio") ?? 1.0;
                    tracing.SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(samplingRatio)));
                }
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        return builder;
    }

    /// <summary>
    /// Реєструє базовий Health Check перевірки життєдіяльності застосунку.
    /// </summary>
    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    /// <summary>
    /// Підключає middleware для перехоплення та прокидання CorrelationId.
    /// </summary>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }

    /// <summary>
    /// Мапить стандартизовані ендпоінти перевірки здоров'я:
    /// /health/live (Liveness) - перевірка лише життєздатності процесу
    /// /health/ready (Readiness) - перевірка готовності зовнішніх залежностей (БД, кеш, downstream gRPC)
    /// /health та /alive - для зворотної сумісності.
    /// Усі ендпоінти повертають структурований JSON (Критерій 11).
    /// </summary>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // 1. Liveness probe (/health/live): перевірка лише працездатності процесу застосунку без зовнішніх залежностей
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live") || r.Name.Equals("self", StringComparison.OrdinalIgnoreCase),
            ResponseWriter = HealthResponseWriter.WriteJsonResponse
        });

        // 2. Readiness probe (/health/ready): перевірка готовності приймати трафік (БД, Redis, downstream gRPC)
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("ready"),
            ResponseWriter = HealthResponseWriter.WriteJsonResponse
        });

        // 3. Зворотна сумісність для стандартних ендпоінтів платформи
        app.MapHealthChecks(HealthEndpointPath, new HealthCheckOptions
        {
            ResponseWriter = HealthResponseWriter.WriteJsonResponse
        });

        app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live") || r.Name.Equals("self", StringComparison.OrdinalIgnoreCase),
            ResponseWriter = HealthResponseWriter.WriteJsonResponse
        });

        return app;
    }
}
