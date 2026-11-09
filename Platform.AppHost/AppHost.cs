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

// Сховище Redis для дворівневого кешування (L2) (Критерій 1)
var redis = builder.AddRedis("redis")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

// Мікросервіси платформи
var bookings = builder.AddProject<Projects.Bookings_Api>("bookings-api")
    .WithReference(bookingsDb).WaitFor(bookingsDb)
    .WithReference(redis).WaitFor(redis);

var catalog = builder.AddProject<Projects.Catalog_Api>("catalog-api")
    .WithReference(catalogDb).WaitFor(catalogDb)
    .WithReference(redis).WaitFor(redis);

var reviews = builder.AddProject<Projects.Reviews_WebApi>("reviews-api")
    .WithReference(reviewsDb).WaitFor(reviewsDb)
    .WithReference(redis).WaitFor(redis);

// Агрегатор залежить від усіх трьох сервісів та Redis
var aggregator = builder.AddProject<Projects.Platform_Aggregator>("aggregator")
    .WithReference(bookings).WithReference(catalog).WithReference(reviews)
    .WithReference(redis)
    .WaitFor(bookings).WaitFor(catalog).WaitFor(reviews).WaitFor(redis);

// API Gateway на фіксованому порті 5000, залежить від сервісів та агрегатора
builder.AddProject<Projects.Platform_ApiGateway>("gateway")
    .WithHttpEndpoint(port: 5000, name: "public")
    .WithExternalHttpEndpoints()
    .WithReference(bookings).WithReference(catalog).WithReference(reviews)
    .WithReference(aggregator)
    .WaitFor(aggregator);

builder.Build().Run();
