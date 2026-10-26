namespace Reviews.Infrastructure.Seeding;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Reviews.Infrastructure.Persistence;

/// <summary>
/// Фоновий сервіс створення необхідних індексів у MongoDB при запуску додатку (Критерій 14).
/// Ідемпотентне створення індексів:
/// - складений індекс { serviceId: 1, createdAt: -1 }
/// - повнотекстовий індекс { title: "text", text: "text" }
/// - унікальний індекс { serviceId: 1, "author.clientId": 1 }
/// </summary>
public class IndexCreationService : IHostedService
{
    private readonly MongoDbContext _context;
    private readonly ILogger<IndexCreationService> _logger;

    public IndexCreationService(MongoDbContext context, ILogger<IndexCreationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Початок перевірки та створення індексів для колекції reviews...");

            var indexModelServiceCreatedAt = new CreateIndexModel<Review>(
                Builders<Review>.IndexKeys.Ascending(r => r.ServiceId).Descending(r => r.CreatedAt),
                new CreateIndexOptions { Name = "ix_reviews_serviceId_createdAt" });

            var indexModelText = new CreateIndexModel<Review>(
                Builders<Review>.IndexKeys.Text(r => r.Title).Text(r => r.Text),
                new CreateIndexOptions { Name = "ix_reviews_title_text" });

            var indexModelUniqueAuthor = new CreateIndexModel<Review>(
                Builders<Review>.IndexKeys.Ascending(r => r.ServiceId).Ascending("author.clientId"),
                new CreateIndexOptions { Name = "ux_reviews_serviceId_authorClientId", Unique = true });

            await _context.Reviews.Indexes.CreateManyAsync(
                new[] { indexModelServiceCreatedAt, indexModelText, indexModelUniqueAuthor },
                cancellationToken);

            _logger.LogInformation("Індекси для колекції reviews успішно створено/актуалізовано.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не вдалося створити індекси MongoDB при старті (можливо сервер недоступний). Продовження роботи.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
