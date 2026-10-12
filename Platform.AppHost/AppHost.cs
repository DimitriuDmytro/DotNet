var builder = DistributedApplication.CreateBuilder(args);

// Варіант 1 (за замовчуванням): існуюча БД з Лаб. 01
var bookingsDb = builder.AddConnectionString("BookingsDb");

// Варіант 2 (контейнер SQL Server через Aspire):
// var sql = builder.AddSqlServer("sql").WithDataVolume();
// var bookingsDb = sql.AddDatabase("BookingsDb");

builder.AddProject<Projects.Bookings_Api>("bookings-api")
    .WithReference(bookingsDb);

builder.Build().Run();
