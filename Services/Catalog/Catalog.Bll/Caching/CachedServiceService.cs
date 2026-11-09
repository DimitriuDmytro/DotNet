namespace Catalog.Bll.Caching;

using Catalog.Bll.DTOs;
using Catalog.Bll.Interfaces;
using Catalog.Bll.Services;
using Microsoft.Extensions.Logging;

/// <summary>
/// Декоратор для IServiceService з реалізацією патерну Cache-Aside
/// поверх дворівневого кешу (L1 IMemoryCache + L2 Redis IDistributedCache).
/// Читання кешуються, запис викликає оригінальний метод та синхронно інвалідує кеш.
/// </summary>
public sealed class CachedServiceService : IServiceService
{
    private readonly ServiceService _inner;
    private readonly ITwoLevelCache _cache;
    private readonly ILogger<CachedServiceService> _logger;

    public CachedServiceService(
        ServiceService inner,
        ITwoLevelCache cache,
        ILogger<CachedServiceService> logger)
    {
        _inner = inner;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ServiceDetailDto> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var key = CatalogCacheKeys.Service(id);
        var cached = await _cache.GetOrCreateAsync(
            key,
            async ct => await _inner.GetByIdAsync(id, ct),
            CatalogCacheTtl.Service,
            cancellationToken);

        return cached!;
    }

    public async Task<ServiceDetailDto> GetByIdExplicitAsync(long id, CancellationToken cancellationToken = default)
    {
        var key = CatalogCacheKeys.Service(id);
        var cached = await _cache.GetOrCreateAsync(
            key,
            async ct => await _inner.GetByIdExplicitAsync(id, ct),
            CatalogCacheTtl.Service,
            cancellationToken);

        return cached!;
    }

    public async Task<PagedResult<ServiceDto>> GetPagedAsync(ServiceQuery query, CancellationToken cancellationToken = default)
    {
        var key = CatalogCacheKeys.ServicesPage(query.Page, query.PageSize, query.CategoryId, query.Search, $"{query.SortBy}_{query.SortDir}");
        var cached = await _cache.GetOrCreateAsync(
            key,
            async ct => await _inner.GetPagedAsync(query, ct),
            CatalogCacheTtl.ServicesList,
            cancellationToken);

        return cached!;
    }

    public async Task<IReadOnlyList<ServiceDto>> GetWithMinWarrantyAsync(int minWarrantyMonths, CancellationToken cancellationToken = default)
    {
        var key = CatalogCacheKeys.MinWarranty(minWarrantyMonths);
        var cached = await _cache.GetOrCreateAsync(
            key,
            async ct => await _inner.GetWithMinWarrantyAsync(minWarrantyMonths, ct),
            CatalogCacheTtl.ServicesList,
            cancellationToken);

        return cached ?? Array.Empty<ServiceDto>();
    }

    public async Task<ServiceDetailDto> CreateAsync(CreateServiceDto dto, CancellationToken cancellationToken = default)
    {
        var result = await _inner.CreateAsync(dto, cancellationToken);
        await InvalidateServiceAsync(result.Id, $"Service created with id {result.Id}", cancellationToken);
        return result;
    }

    public async Task<ServiceDetailDto> UpdateAsync(long id, UpdateServiceDto dto, CancellationToken cancellationToken = default)
    {
        var result = await _inner.UpdateAsync(id, dto, cancellationToken);
        await InvalidateServiceAsync(id, $"Service updated {id}", cancellationToken);
        return result;
    }

    public async Task<ServiceDetailDto> UpdatePriceAsync(long id, UpdatePriceDto dto, CancellationToken cancellationToken = default)
    {
        var result = await _inner.UpdatePriceAsync(id, dto, cancellationToken);
        await InvalidateServiceAsync(id, $"Service price updated {id}", cancellationToken);
        return result;
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        await _inner.DeleteAsync(id, cancellationToken);
        await InvalidateServiceAsync(id, $"Service deleted {id}", cancellationToken);
    }

    public async Task<ServiceImageDto> AddImageAsync(long serviceId, AddServiceImageDto dto, CancellationToken cancellationToken = default)
    {
        var result = await _inner.AddImageAsync(serviceId, dto, cancellationToken);
        await InvalidateServiceAsync(serviceId, $"Service image added {serviceId}", cancellationToken);
        return result;
    }

    public async Task RemoveImageAsync(long serviceId, long imageId, CancellationToken cancellationToken = default)
    {
        await _inner.RemoveImageAsync(serviceId, imageId, cancellationToken);
        await InvalidateServiceAsync(serviceId, $"Service image removed {serviceId}", cancellationToken);
    }

    public async Task<ServiceSpecDto> UpsertSpecAsync(long serviceId, UpsertServiceSpecDto dto, CancellationToken cancellationToken = default)
    {
        var result = await _inner.UpsertSpecAsync(serviceId, dto, cancellationToken);
        await InvalidateServiceAsync(serviceId, $"Service spec upserted {serviceId}", cancellationToken);
        return result;
    }

    private async Task InvalidateServiceAsync(long id, string reason, CancellationToken ct)
    {
        await _cache.RemoveAsync(CatalogCacheKeys.Service(id), reason, ct);
        _logger.LogInformation("Invalidated cache for service {ServiceId}: {Reason}", id, reason);
    }
}
