namespace Reviews.Infrastructure.Persistence;

/// <summary>
/// Опції конфігурації підключення до MongoDB (Критерій 10, 14).
/// </summary>
public class MongoDbOptions
{
    public const string SectionName = "MongoDb";

    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "detailing_reviews";
    public bool RunStartupTasks { get; set; } = true;
}
