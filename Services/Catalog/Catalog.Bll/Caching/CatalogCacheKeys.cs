namespace Catalog.Bll.Caching;

public static class CatalogCacheKeys
{
    public const string Prefix = "catalog";
    public const string Categories = "catalog:categories";
    public const string ServicesVersion = "catalog:services:ver";

    public static string Service(long id) => $"catalog:service:{id}";

    public static string ServicesPage(int page, int size, long? categoryId, string? search, decimal? minPrice, decimal? maxPrice, string? sort, string version = "1")
    {
        var categoryPart = categoryId.HasValue ? categoryId.Value.ToString() : "all";
        var searchPart = string.IsNullOrWhiteSpace(search) ? "none" : search.Trim().ToLowerInvariant();
        var pricePart = $"{minPrice?.ToString() ?? "min"}_{maxPrice?.ToString() ?? "max"}";
        var sortPart = string.IsNullOrWhiteSpace(sort) ? "default" : sort.Trim().ToLowerInvariant();
        return $"catalog:services:v{version}:p{page}:s{size}:c{categoryPart}:q{searchPart}:pr{pricePart}:sort{sortPart}";
    }

    public static string MinWarranty(int months) => $"catalog:services:warranty:{months}";
}

public static class CatalogCacheTtl
{
    // Окремий запис послуги: sliding 1 хв, absolute 5 хв у L1; 30 хв у L2
    public static readonly CacheEntrySettings Service = new(
        L1Ttl: TimeSpan.FromMinutes(5),
        L2Ttl: TimeSpan.FromMinutes(30),
        L1Sliding: TimeSpan.FromMinutes(1),
        Size: 1);

    // Довідник категорій: рідко змінюється, absolute 10 хв у L1; 1 год у L2
    public static readonly CacheEntrySettings Categories = new(
        L1Ttl: TimeSpan.FromMinutes(10),
        L2Ttl: TimeSpan.FromHours(1),
        L1Sliding: null,
        Size: 2);

    // Списки послуг та фільтри: 30 с у L1; 5 хв у L2
    public static readonly CacheEntrySettings ServicesList = new(
        L1Ttl: TimeSpan.FromSeconds(30),
        L2Ttl: TimeSpan.FromMinutes(5),
        L1Sliding: null,
        Size: 3);
}
