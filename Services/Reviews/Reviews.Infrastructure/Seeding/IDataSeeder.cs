namespace Reviews.Infrastructure.Seeding;

/// <summary>
/// Інтерфейс ідемпотентного наповнення бази даних початковими даними (Критерій 14).
/// </summary>
public interface IDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
