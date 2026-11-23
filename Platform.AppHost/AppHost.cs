var builder = DistributedApplication.CreateBuilder(args);

// Пароль SQL Server передається тільки як параметр-секрет
var sqlPassword = builder.AddParameter("sql-password", secret: true);
var sql = builder.AddSqlServer("sql", sqlPassword)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var bookingsDb = sql.AddDatabase("BookingsDb");
var catalogDb = sql.AddDatabase("CatalogDb");

// База даних MongoDB для відгуків та звітів
var mongo = builder.AddMongoDB("mongo")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var reviewsDb = mongo.AddDatabase("ReviewsDb");

// Сховище Redis для дворівневого кешування (L2)
var redis = builder.AddRedis("redis")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

// Брокер повідомлень RabbitMQ з Management UI та персистентним volume (Лаб. 08, Критерій 1)
var rabbit = builder.AddRabbitMQ("messaging")
    .WithDataVolume()
    .WithManagementPlugin();

// Критерій 12: Налаштування WithHttpHealthCheck("/health/ready") та ланцюжків WaitFor

// 1. Мікросервіс бронювань
var bookings = builder.AddProject<Projects.Bookings_Api>("bookings-api")
    .WithReference(bookingsDb).WaitFor(bookingsDb)
    .WithReference(redis).WaitFor(redis)
    .WithReference(rabbit).WaitFor(rabbit)
    .WithHttpHealthCheck("/health/ready");

// 2. Мікросервіс каталогу послуг
var catalog = builder.AddProject<Projects.Catalog_Api>("catalog-api")
    .WithReference(catalogDb).WaitFor(catalogDb)
    .WithReference(redis).WaitFor(redis)
    .WithReference(rabbit).WaitFor(rabbit)
    .WithHttpHealthCheck("/health/ready");

// 3. Мікросервіс відгуків
var reviews = builder.AddProject<Projects.Reviews_WebApi>("reviews-api")
    .WithReference(reviewsDb).WaitFor(reviewsDb)
    .WithReference(redis).WaitFor(redis)
    .WithReference(rabbit).WaitFor(rabbit)
    .WithHttpHealthCheck("/health/ready");

// 4. Композитний сервіс агрегації (Aggregator)
var aggregator = builder.AddProject<Projects.Platform_Aggregator>("aggregator")
    .WithReference(bookings).WithReference(catalog).WithReference(reviews)
    .WithReference(redis)
    .WaitFor(bookings).WaitFor(catalog).WaitFor(reviews).WaitFor(redis)
    .WithHttpHealthCheck("/health/ready");

// 5. API Gateway на фіксованому порті 5000, залежить від сервісів та агрегатора
builder.AddProject<Projects.Platform_ApiGateway>("gateway")
    .WithHttpEndpoint(port: 5000, name: "public")
    .WithExternalHttpEndpoints()
    .WithReference(bookings).WithReference(catalog).WithReference(reviews)
    .WithReference(aggregator)
    .WaitFor(aggregator).WaitFor(bookings).WaitFor(catalog).WaitFor(reviews)
    .WithHttpHealthCheck("/health/ready");

builder.Build().Run();
