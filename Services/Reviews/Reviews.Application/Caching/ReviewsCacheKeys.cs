namespace Reviews.Application.Caching;

public static class ReviewsCacheKeys
{
    public const string Prefix = "reviews";

    public static string Review(string id) => $"reviews:review:{id}";

    public static string ServiceReviews(long serviceId, int page = 1, int pageSize = 20) =>
        $"reviews:service:{serviceId}:p{page}:s{pageSize}";

    public static string ReviewsList(int page = 1, int pageSize = 20) =>
        $"reviews:list:p{page}:s{pageSize}";
}

public static class ReviewsCacheTtl
{
    // Окремий відгук: 5 хв у L1, 30 хв у L2, sliding 1 хв
    public static readonly CacheEntrySettings Review = new(
        L1Ttl: TimeSpan.FromMinutes(5),
        L2Ttl: TimeSpan.FromMinutes(30),
        L1Sliding: TimeSpan.FromMinutes(1),
        Size: 1);

    // Списки відгуків: 1 хв у L1, 10 хв у L2
    public static readonly CacheEntrySettings ReviewsList = new(
        L1Ttl: TimeSpan.FromMinutes(1),
        L2Ttl: TimeSpan.FromMinutes(10),
        L1Sliding: null,
        Size: 2);
}
