var builder = DistributedApplication.CreateBuilder(args);

// Пароль SQL Server передається тільки як параметр-секрет
var sqlPassword = builder.AddParameter("sql-password", secret: true);
var sql = builder.AddSqlServer("sql", sqlPassword).WithDataVolume();
var bookingsDb = sql.AddDatabase("BookingsDb");
var catalogDb = sql.AddDatabase("CatalogDb");

// База даних MongoDB для відгуків та звітів
var mongo = builder.AddMongoDB("mongo").WithDataVolume();
var reviewsDb = mongo.AddDatabase("ReviewsDb");

// Мікросервіси платформи
var bookings = builder.AddProject<Projects.Bookings_Api>("bookings-api")
    .WithReference(bookingsDb).WaitFor(bookingsDb);

var catalog = builder.AddProject<Projects.Catalog_Api>("catalog-api")
    .WithReference(catalogDb).WaitFor(catalogDb);

var reviews = builder.AddProject<Projects.Reviews_WebApi>("reviews-api")
    .WithReference(reviewsDb).WaitFor(reviewsDb);

// Агрегатор залежить від усіх трьох сервісів і чекає їх готовності
var aggregator = builder.AddProject<Projects.Platform_Aggregator>("aggregator")
    .WithReference(bookings).WithReference(catalog).WithReference(reviews)
    .WaitFor(bookings).WaitFor(catalog).WaitFor(reviews);

// API Gateway на фіксованому порті 5000, залежить від сервісів та агрегатора
builder.AddProject<Projects.Platform_ApiGateway>("gateway")
    .WithHttpEndpoint(port: 5000, name: "public")
    .WithReference(bookings).WithReference(catalog).WithReference(reviews)
    .WithReference(aggregator)
    .WaitFor(aggregator);

builder.Build().Run();
