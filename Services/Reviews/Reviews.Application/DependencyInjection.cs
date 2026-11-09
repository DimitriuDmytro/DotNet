namespace Reviews.Application;

using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Common.Behaviors;

/// <summary>
/// Реєстрація сервісів шару Application у DI-контейнері: MediatR пайплайн із кешуванням та валідацією.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration? configuration = null)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Дворівневий кеш (L1 Memory + L2 Redis)
        services.AddSingleton<ReviewsCacheMetrics>();
        services.AddSingleton<IReviewsTwoLevelCache, ReviewsTwoLevelCache>();
        services.AddTransient<ReviewsCacheHealthCheck>();

        services.AddMediatR(cfg =>
        {
            var licenseKey = configuration?["LuckyPenny:LicenseKey"];
            if (!string.IsNullOrWhiteSpace(licenseKey))
            {
                cfg.LicenseKey = licenseKey;
            }

            cfg.RegisterServicesFromAssembly(assembly);

            // Порядок реєстрації поведінок пайплайну MediatR:
            // 1. LoggingBehavior
            // 2. ValidationBehavior
            // 3. PerformanceBehavior
            // 4. CachingBehavior (читання/повернення з кешу до виклику хендлера)
            // 5. CacheInvalidationBehavior (інвалідація кешу після успішного виконання команди)
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));
            cfg.AddOpenBehavior(typeof(CachingBehavior<,>));
            cfg.AddOpenBehavior(typeof(CacheInvalidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        services.AddAutoMapper(cfg =>
        {
            var licenseKey = configuration?["LuckyPenny:LicenseKey"];
            if (!string.IsNullOrWhiteSpace(licenseKey))
            {
                cfg.LicenseKey = licenseKey;
            }
        }, assembly);

        return services;
    }
}
