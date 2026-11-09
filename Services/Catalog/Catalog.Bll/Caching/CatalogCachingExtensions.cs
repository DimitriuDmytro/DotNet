namespace Catalog.Bll.Caching;

using Catalog.Bll.Interfaces;
using Catalog.Bll.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public static class CatalogCachingExtensions
{
    public static IServiceCollection AddCatalogCaching(this IServiceCollection services)
    {
        services.AddSingleton<CacheMetrics>();
        services.AddSingleton<ITwoLevelCache, TwoLevelCache>();
        services.AddTransient<CacheHealthCheck>();

        services.AddScoped<ServiceService>();
        services.AddScoped<IServiceService, CachedServiceService>();

        services.AddHostedService<CacheWarmupService>();
        return services;
    }
}
