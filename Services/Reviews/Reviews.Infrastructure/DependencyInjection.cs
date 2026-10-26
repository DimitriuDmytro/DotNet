namespace Reviews.Infrastructure;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reviews.Infrastructure.Persistence;
using Reviews.Infrastructure.Seeding;

/// <summary>
/// Реєстрація інфраструктурних сервісів (MongoDB, репозиторії, сідінг, фонові служби) у DI-контейнері (Критерій 7, 10, 14).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        MongoMappings.Register();

        services.Configure<MongoDbOptions>(configuration.GetSection(MongoDbOptions.SectionName));

        services.AddSingleton<MongoDbContext>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IReviewQueries, ReviewQueries>();
        services.AddScoped<IDataSeeder, ReviewSeeder>();
        services.AddHostedService<IndexCreationService>();

        return services;
    }
}
