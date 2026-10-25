var builder = DistributedApplication.CreateBuilder(args);

// Варіант 1 (за замовчуванням): існуючі БД
var bookingsDb = builder.AddConnectionString("BookingsDb");
var catalogDb = builder.AddConnectionString("CatalogDb");

// Варіант 2 (контейнер SQL Server через Aspire):
// var sql = builder.AddSqlServer("sql").WithDataVolume();
// var bookingsDb = sql.AddDatabase("BookingsDb");
// var catalogDb = sql.AddDatabase("CatalogDb");

builder.AddProject<Projects.Bookings_Api>("bookings-api")
    .WithReference(bookingsDb);

builder.AddProject<Projects.Catalog_Api>("catalog-api")
    .WithReference(catalogDb);

builder.Build().Run();
