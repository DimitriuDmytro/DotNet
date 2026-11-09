namespace Catalog.Bll.Caching;

using Catalog.Bll.DTOs;
using Catalog.Bll.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// Фоновий сервіс для прогріву кешу Catalog при запуску:
/// - Довідник категорій послуг
/// - Перша сторінка послуг за замовчуванням
/// - Топ популярних послуг
/// </summary>
public sealed class CacheWarmupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CacheWarmupService> _logger;

    public CacheWarmupService(IServiceProvider serviceProvider, ILogger<CacheWarmupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting cache warmup for Catalog microservice...");
        try
        {
            // Коротка затримка, щоб дозволити базі даних та хосту ініціалізуватися
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

            using var scope = _serviceProvider.CreateScope();
            var serviceService = scope.ServiceProvider.GetRequiredService<IServiceService>();
            var categoryService = scope.ServiceProvider.GetRequiredService<IServiceCategoryService>();
            var cache = scope.ServiceProvider.GetRequiredService<ITwoLevelCache>();

            // 1. Прогрів категорій
            _logger.LogInformation("Warming up categories in cache...");
            await cache.GetOrCreateAsync(
                CatalogCacheKeys.Categories,
                async ct => await categoryService.GetAllAsync(ct),
                CatalogCacheTtl.Categories,
                stoppingToken);

            // 2. Прогрів першої сторінки послуг за замовчуванням
            _logger.LogInformation("Warming up top services page in cache...");
            var defaultQuery = new ServiceQuery { Page = 1, PageSize = 10 };
            var paged = await serviceService.GetPagedAsync(defaultQuery, stoppingToken);

            // 3. Прогрів окремих перших послуг
            if (paged?.Items is not null)
            {
                foreach (var item in paged.Items.Take(5))
                {
                    try
                    {
                        await serviceService.GetByIdAsync(item.Id, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to warm up service {ServiceId}", item.Id);
                    }
                }
            }

            _logger.LogInformation("Cache warmup completed successfully for Catalog.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Cache warmup canceled.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache warmup encountered an error (continuing without warmed cache)");
        }
    }
}
