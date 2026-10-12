var builder = DistributedApplication.CreateBuilder(args);

// =========================================================================
// Варіант 1 (Активний): Підключення до вже розгорнутої локальної БД SQL Server
// Ім'я ресурсу "BookingsDb" збігається з GetConnectionString("BookingsDb") у Bookings.Api
// =========================================================================
var bookingsDb = builder.AddConnectionString("BookingsDb");

// =========================================================================
// Варіант 2 (Альтернативний — у контейнері Docker):
// Розкоментуйте для автоматичного підняття контейнера SQL Server через Aspire
// =========================================================================
// var sqlServer = builder.AddSqlServer("sql-bookings")
//     .WithDataVolume("detailing-bookings-data");
// var bookingsDb = sqlServer.AddDatabase("BookingsDb", "DetailingBookings");

builder.AddProject<Projects.Bookings_Api>("bookings-api")
    .WithReference(bookingsDb);
    // .WaitFor(bookingsDb); // використовується разом із Варіантом 2 (контейнер)

builder.Build().Run();
