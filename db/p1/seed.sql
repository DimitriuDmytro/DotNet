/* =========================================================================
   Проєкт №1: Контекст "Записи" (Bookings)
   Файл: seed.sql
   База даних: DetailingBookings
   Опис: Ідемпотентне наповнення бази даних початковими даними:
         - 5 клієнтів (Clients)
         - 10 локальних послуг (Services — репліка з DetailingCatalog, Id 1..10)
         - 8 записів у різних статусах (Appointments + Details + Items + History)
         - Платежі (Payments)
         Повторний запуск скрипту не викликає помилок і не створює дублікатів.
   ========================================================================= */

USE [DetailingBookings];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

PRINT N'=== Початок заповнення бази [DetailingBookings] (seed.sql) ===';

/* -------------------------------------------------------------------------
   1. Клієнти (5 клієнтів, фіксовані Id 1..5 через IDENTITY_INSERT)
   ------------------------------------------------------------------------- */
SET IDENTITY_INSERT dbo.Clients ON;

IF NOT EXISTS (SELECT 1 FROM dbo.Clients WHERE Id = 1 OR Email = N'oleksandr.kovalenko@example.ua')
BEGIN
    INSERT INTO dbo.Clients (Id, Email, FullName, Phone, CreatedAt, CreatedBy, IsDeleted)
    VALUES (1, N'oleksandr.kovalenko@example.ua', N'Олександр Коваленко', N'+380501112233', '2026-09-01T09:00:00', N'admin.iryna', 0);
END

IF NOT EXISTS (SELECT 1 FROM dbo.Clients WHERE Id = 2 OR Email = N'maryna.shevchenko@example.ua')
BEGIN
    INSERT INTO dbo.Clients (Id, Email, FullName, Phone, CreatedAt, CreatedBy, IsDeleted)
    VALUES (2, N'maryna.shevchenko@example.ua', N'Марина Шевченко', N'+380672223344', '2026-09-03T10:30:00', N'admin.iryna', 0);
END

IF NOT EXISTS (SELECT 1 FROM dbo.Clients WHERE Id = 3 OR Email = N'dmytro.bondarenko@example.ua')
BEGIN
    INSERT INTO dbo.Clients (Id, Email, FullName, Phone, CreatedAt, CreatedBy, IsDeleted)
    VALUES (3, N'dmytro.bondarenko@example.ua', N'Дмитро Бондаренко', N'+380633334455', '2026-09-05T14:15:00', N'admin.maksym', 0);
END

IF NOT EXISTS (SELECT 1 FROM dbo.Clients WHERE Id = 4 OR Email = N'vitalii.tkachenko@example.ua')
BEGIN
    INSERT INTO dbo.Clients (Id, Email, FullName, Phone, CreatedAt, CreatedBy, IsDeleted)
    VALUES (4, N'vitalii.tkachenko@example.ua', N'Віталій Ткаченко', N'+380974445566', '2026-09-10T11:20:00', N'admin.maksym', 0);
END

IF NOT EXISTS (SELECT 1 FROM dbo.Clients WHERE Id = 5 OR Email = N'olena.melnyk@example.ua')
BEGIN
    INSERT INTO dbo.Clients (Id, Email, FullName, Phone, CreatedAt, CreatedBy, IsDeleted)
    VALUES (5, N'olena.melnyk@example.ua', N'Олена Мельник', N'+380935556677', '2026-09-12T16:45:00', N'admin.iryna', 0);
END

SET IDENTITY_INSERT dbo.Clients OFF;
PRINT N'Клієнти (5 записів) перевірені/додані.';
GO

/* -------------------------------------------------------------------------
   2. Локальна копія довідника послуг Services (10 послуг, Id БЕЗ IDENTITY)
   Ідентифікатори Id 1..10 повністю узгоджені з базою DetailingCatalog.
   ------------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE Id = 1)
    INSERT INTO dbo.Services (Id, Name, Price, DurationMin) VALUES (1, N'Двофазна детейлінг-мийка', 800.00, 60);

IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE Id = 2)
    INSERT INTO dbo.Services (Id, Name, Price, DurationMin) VALUES (2, N'Трифазна нано-мийка з консервацією', 1200.00, 90);

IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE Id = 3)
    INSERT INTO dbo.Services (Id, Name, Price, DurationMin) VALUES (3, N'Деконтамінація кузова та дисків', 1500.00, 120);

IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE Id = 4)
    INSERT INTO dbo.Services (Id, Name, Price, DurationMin) VALUES (4, N'Легке відновлювальне полірування', 4500.00, 360);

IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE Id = 5)
    INSERT INTO dbo.Services (Id, Name, Price, DurationMin) VALUES (5, N'Глибоке абразивне полірування кузова', 8500.00, 600);

IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE Id = 6)
    INSERT INTO dbo.Services (Id, Name, Price, DurationMin) VALUES (6, N'Полірування фар та ліхтарів', 1400.00, 90);

IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE Id = 7)
    INSERT INTO dbo.Services (Id, Name, Price, DurationMin) VALUES (7, N'Керамічне покриття кузова 9H', 12000.00, 720);

IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE Id = 8)
    INSERT INTO dbo.Services (Id, Name, Price, DurationMin) VALUES (8, N'Антидощ на всі вікна', 1800.00, 120);

IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE Id = 9)
    INSERT INTO dbo.Services (Id, Name, Price, DurationMin) VALUES (9, N'Повна хімчистка салону з розбором', 5500.00, 480);

IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE Id = 10)
    INSERT INTO dbo.Services (Id, Name, Price, DurationMin) VALUES (10, N'Озонування та антибактеріальна обробка', 900.00, 45);

PRINT N'Локальний довідник послуг Services (10 записів) перевірено/додано.';
GO

/* -------------------------------------------------------------------------
   3. Записи на обслуговування (8 записів у різних статусах, Id 1..8)
   ------------------------------------------------------------------------- */
SET IDENTITY_INSERT dbo.Appointments ON;

-- Запис #1: Delivered (Олександр Коваленко, BMW X5, мийка + кераміка = 12300.00 зі знижкою 500)
IF NOT EXISTS (SELECT 1 FROM dbo.Appointments WHERE Id = 1)
BEGIN
    INSERT INTO dbo.Appointments (Id, ClientId, Status, ScheduledAt, TotalAmount, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)
    VALUES (1, 1, N'Delivered', '2026-10-01T09:00:00', 12300.00, '2026-09-28T12:00:00', N'admin.iryna', '2026-10-02T18:00:00', N'admin.iryna', 0);
END

-- Запис #2: Delivered (Марина Шевченко, Audi Q7, мийка + хімчистка + озонування = 7000.00 зі знижкою 200)
IF NOT EXISTS (SELECT 1 FROM dbo.Appointments WHERE Id = 2)
BEGIN
    INSERT INTO dbo.Appointments (Id, ClientId, Status, ScheduledAt, TotalAmount, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)
    VALUES (2, 2, N'Delivered', '2026-10-03T10:00:00', 7000.00, '2026-09-30T15:20:00', N'admin.iryna', '2026-10-04T17:30:00', N'admin.maksym', 0);
END

-- Запис #3: Ready (Дмитро Бондаренко, Porsche Cayenne, трифазна мийка + глибоке полірування + антидощ = 11500.00)
IF NOT EXISTS (SELECT 1 FROM dbo.Appointments WHERE Id = 3)
BEGIN
    INSERT INTO dbo.Appointments (Id, ClientId, Status, ScheduledAt, TotalAmount, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)
    VALUES (3, 3, N'Ready', '2026-10-10T09:30:00', 11500.00, '2026-10-07T11:00:00', N'admin.maksym', '2026-10-11T16:00:00', N'master.andrii', 0);
END

-- Запис #4: QualityCheck (Віталій Ткаченко, Mercedes-Benz E300, легке полірування + полірування фар = 5900.00)
IF NOT EXISTS (SELECT 1 FROM dbo.Appointments WHERE Id = 4)
BEGIN
    INSERT INTO dbo.Appointments (Id, ClientId, Status, ScheduledAt, TotalAmount, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)
    VALUES (4, 4, N'QualityCheck', '2026-10-11T10:00:00', 5900.00, '2026-10-08T14:00:00', N'admin.iryna', '2026-10-11T18:30:00', N'master.taras', 0);
END

-- Запис #5: InProgress (Олена Мельник, Tesla Model Y, кераміка + антидощ = 13500.00 зі знижкою 300)
IF NOT EXISTS (SELECT 1 FROM dbo.Appointments WHERE Id = 5)
BEGIN
    INSERT INTO dbo.Appointments (Id, ClientId, Status, ScheduledAt, TotalAmount, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)
    VALUES (5, 5, N'InProgress', '2026-10-12T08:30:00', 13500.00, '2026-10-09T10:00:00', N'admin.maksym', '2026-10-12T09:15:00', N'master.andrii', 0);
END

-- Запис #6: CarReceived (Олександр Коваленко, Volkswagen Touareg, двофазна мийка + деконтамінація = 2300.00)
IF NOT EXISTS (SELECT 1 FROM dbo.Appointments WHERE Id = 6)
BEGIN
    INSERT INTO dbo.Appointments (Id, ClientId, Status, ScheduledAt, TotalAmount, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)
    VALUES (6, 1, N'CarReceived', '2026-10-12T11:00:00', 2300.00, '2026-10-10T16:00:00', N'admin.iryna', '2026-10-12T10:55:00', N'admin.iryna', 0);
END

-- Запис #7: Booked (Марина Шевченко, Audi Q7, трифазна мийка + полірування фар = 2600.00)
IF NOT EXISTS (SELECT 1 FROM dbo.Appointments WHERE Id = 7)
BEGIN
    INSERT INTO dbo.Appointments (Id, ClientId, Status, ScheduledAt, TotalAmount, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)
    VALUES (7, 2, N'Booked', '2026-10-15T10:00:00', 2600.00, '2026-10-11T12:30:00', N'admin.iryna', NULL, NULL, 0);
END

-- Запис #8: Cancelled (Дмитро Бондаренко, Toyota Camry, двофазна мийка + озонування = 1700.00)
IF NOT EXISTS (SELECT 1 FROM dbo.Appointments WHERE Id = 8)
BEGIN
    INSERT INTO dbo.Appointments (Id, ClientId, Status, ScheduledAt, TotalAmount, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)
    VALUES (8, 3, N'Cancelled', '2026-10-06T13:00:00', 1700.00, '2026-10-04T09:30:00', N'admin.maksym', '2026-10-05T18:00:00', N'admin.maksym', 0);
END

SET IDENTITY_INSERT dbo.Appointments OFF;
PRINT N'Записи Appointments (8 записів) перевірені/додані.';
GO

/* -------------------------------------------------------------------------
   4. Деталі автомобілів AppointmentDetails (1:1 до Appointments)
   ------------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentDetails WHERE AppointmentId = 1)
    INSERT INTO dbo.AppointmentDetails (AppointmentId, CarBrand, CarModel, PlateNumber, Comment)
    VALUES (1, N'BMW', N'X5 G05', N'AA7777OO', N'Чорний сапфір, прохання звернути увагу на капот і передній бампер.');

IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentDetails WHERE AppointmentId = 2)
    INSERT INTO dbo.AppointmentDetails (AppointmentId, CarBrand, CarModel, PlateNumber, Comment)
    VALUES (2, N'Audi', N'Q7', N'KA2450CH', N'Світла шкіра салону, є плями від кави на задньому ряду.');

IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentDetails WHERE AppointmentId = 3)
    INSERT INTO dbo.AppointmentDetails (AppointmentId, CarBrand, CarModel, PlateNumber, Comment)
    VALUES (3, N'Porsche', N'Cayenne Coupe', N'AA0911PP', N'Після траси, багато бітуму та слідів комах.');

IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentDetails WHERE AppointmentId = 4)
    INSERT INTO dbo.AppointmentDetails (AppointmentId, CarBrand, CarModel, PlateNumber, Comment)
    VALUES (4, N'Mercedes-Benz', N'E300 W213', N'BC4500HK', N'Помутніння фар, дрібна павутинка після автоматичних мийок.');

IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentDetails WHERE AppointmentId = 5)
    INSERT INTO dbo.AppointmentDetails (AppointmentId, CarBrand, CarModel, PlateNumber, Comment)
    VALUES (5, N'Tesla', N'Model Y Performance', N'KA9900YA', N'Нове авто з салону, нанесення захисної кераміки та антидощу.');

IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentDetails WHERE AppointmentId = 6)
    INSERT INTO dbo.AppointmentDetails (AppointmentId, CarBrand, CarModel, PlateNumber, Comment)
    VALUES (6, N'Volkswagen', N'Touareg', N'AI3311BM', N'Другий автомобіль клієнта, підготовка до осіннього сезону.');

IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentDetails WHERE AppointmentId = 7)
    INSERT INTO dbo.AppointmentDetails (AppointmentId, CarBrand, CarModel, PlateNumber, Comment)
    VALUES (7, N'Audi', N'Q7', N'KA2450CH', N'Плановий догляд після хімчистки та полірування фар.');

IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentDetails WHERE AppointmentId = 8)
    INSERT INTO dbo.AppointmentDetails (AppointmentId, CarBrand, CarModel, PlateNumber, Comment)
    VALUES (8, N'Toyota', N'Camry XV70', N'AA5522KT', N'Клієнт переніс поїздку і скасував візит за день до прийому.');

PRINT N'Деталі авто AppointmentDetails (8 записів) перевірені/додані.';
GO

/* -------------------------------------------------------------------------
   5. Позиції записів AppointmentItems (знімки ServiceName та UnitPrice)
   ------------------------------------------------------------------------- */
-- Запис #1 (800 + 12000 - 500 = 12300)
IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentItems WHERE AppointmentId = 1 AND ServiceId = 1)
    INSERT INTO dbo.AppointmentItems (AppointmentId, ServiceId, ServiceName, UnitPrice, Discount, Quantity)
    VALUES (1, 1, N'Двофазна детейлінг-мийка', 800.00, 0.00, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentItems WHERE AppointmentId = 1 AND ServiceId = 7)
    INSERT INTO dbo.AppointmentItems (AppointmentId, ServiceId, ServiceName, UnitPrice, Discount, Quantity)
    VALUES (1, 7, N'Керамічне покриття кузова 9H', 12000.00, 500.00, 1);

-- Запис #2 (800 + 5500 - 200 + 900 = 7000)
IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentItems WHERE AppointmentId = 2 AND ServiceId = 1)
    INSERT INTO dbo.AppointmentItems (AppointmentId, ServiceId, ServiceName, UnitPrice, Discount, Quantity)
    VALUES (2, 1, N'Двофазна детейлінг-мийка', 800.00, 0.00, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentItems WHERE AppointmentId = 2 AND ServiceId = 9)
    INSERT INTO dbo.AppointmentItems (AppointmentId, ServiceId, ServiceName, UnitPrice, Discount, Quantity)
    VALUES (2, 9, N'Повна хімчистка салону з розбором', 5500.00, 200.00, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentItems WHERE AppointmentId = 2 AND ServiceId = 10)
    INSERT INTO dbo.AppointmentItems (AppointmentId, ServiceId, ServiceName, UnitPrice, Discount, Quantity)
    VALUES (2, 10, N'Озонування та антибактеріальна обробка', 900.00, 0.00, 1);

-- Запис #3 (1200 + 8500 + 1800 = 11500)
IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentItems WHERE AppointmentId = 3 AND ServiceId = 2)
    INSERT INTO dbo.AppointmentItems (AppointmentId, ServiceId, ServiceName, UnitPrice, Discount, Quantity)
    VALUES (3, 2, N'Трифазна нано-мийка з консервацією', 1200.00, 0.00, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentItems WHERE AppointmentId = 3 AND ServiceId = 5)
    INSERT INTO dbo.AppointmentItems (AppointmentId, ServiceId, ServiceName, UnitPrice, Discount, Quantity)
    VALUES (3, 5, N'Глибоке абразивне полірування кузова', 8500.00, 0.00, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentItems WHERE AppointmentId = 3 AND ServiceId = 8)
    INSERT INTO dbo.AppointmentItems (AppointmentId, ServiceId, ServiceName, UnitPrice, Discount, Quantity)
    VALUES (3, 8, N'Антидощ на всі вікна', 1800.00, 0.00, 1);

-- Запис #4 (4500 + 1400 = 5900)
IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentItems WHERE AppointmentId = 4 AND ServiceId = 4)
    INSERT INTO dbo.AppointmentItems (AppointmentId, ServiceId, ServiceName, UnitPrice, Discount, Quantity)
    VALUES (4, 4, N'Легке відновлювальне полірування', 4500.00, 0.00, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentItems WHERE AppointmentId = 4 AND ServiceId = 6)
    INSERT INTO dbo.AppointmentItems (AppointmentId, ServiceId, ServiceName, UnitPrice, Discount, Quantity)
    VALUES (4, 6, N'Полірування фар та ліхтарів', 1400.00, 0.00, 1);

-- Запис #5 (12000 - 300 + 1800 = 13500)
IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentItems WHERE AppointmentId = 5 AND ServiceId = 7)
    INSERT INTO dbo.AppointmentItems (AppointmentId, ServiceId, ServiceName, UnitPrice, Discount, Quantity)
    VALUES (5, 7, N'Керамічне покриття кузова 9H', 12000.00, 300.00, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentItems WHERE AppointmentId = 5 AND ServiceId = 8)
    INSERT INTO dbo.AppointmentItems (AppointmentId, ServiceId, ServiceName, UnitPrice, Discount, Quantity)
    VALUES (5, 8, N'Антидощ на всі вікна', 1800.00, 0.00, 1);

-- Запис #6 (800 + 1500 = 2300)
IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentItems WHERE AppointmentId = 6 AND ServiceId = 1)
    INSERT INTO dbo.AppointmentItems (AppointmentId, ServiceId, ServiceName, UnitPrice, Discount, Quantity)
    VALUES (6, 1, N'Двофазна детейлінг-мийка', 800.00, 0.00, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentItems WHERE AppointmentId = 6 AND ServiceId = 3)
    INSERT INTO dbo.AppointmentItems (AppointmentId, ServiceId, ServiceName, UnitPrice, Discount, Quantity)
    VALUES (6, 3, N'Деконтамінація кузова та дисків', 1500.00, 0.00, 1);

-- Запис #7 (1200 + 1400 = 2600)
IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentItems WHERE AppointmentId = 7 AND ServiceId = 2)
    INSERT INTO dbo.AppointmentItems (AppointmentId, ServiceId, ServiceName, UnitPrice, Discount, Quantity)
    VALUES (7, 2, N'Трифазна нано-мийка з консервацією', 1200.00, 0.00, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentItems WHERE AppointmentId = 7 AND ServiceId = 6)
    INSERT INTO dbo.AppointmentItems (AppointmentId, ServiceId, ServiceName, UnitPrice, Discount, Quantity)
    VALUES (7, 6, N'Полірування фар та ліхтарів', 1400.00, 0.00, 1);

-- Запис #8 (800 + 900 = 1700)
IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentItems WHERE AppointmentId = 8 AND ServiceId = 1)
    INSERT INTO dbo.AppointmentItems (AppointmentId, ServiceId, ServiceName, UnitPrice, Discount, Quantity)
    VALUES (8, 1, N'Двофазна детейлінг-мийка', 800.00, 0.00, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentItems WHERE AppointmentId = 8 AND ServiceId = 10)
    INSERT INTO dbo.AppointmentItems (AppointmentId, ServiceId, ServiceName, UnitPrice, Discount, Quantity)
    VALUES (8, 10, N'Озонування та антибактеріальна обробка', 900.00, 0.00, 1);

PRINT N'Позиції замовлень AppointmentItems перевірені/додані.';
GO

/* -------------------------------------------------------------------------
   6. Платежі Payments
   - Запис #1 (Delivered): повністю оплачений двома платежами (3000 + 9300 = 12300)
   - Запис #2 (Delivered): повністю оплачений одним платежем (7000)
   - Запис #3 (Ready): внесено лише передплату 5000 із 11500 (неповна оплата для тестів!)
   - Запис #5 (InProgress): передплата 3500 із 13500
   ------------------------------------------------------------------------- */
SET IDENTITY_INSERT dbo.Payments ON;

IF NOT EXISTS (SELECT 1 FROM dbo.Payments WHERE Id = 1)
    INSERT INTO dbo.Payments (Id, AppointmentId, Amount, Method, PaidAt)
    VALUES (1, 1, 3000.00, N'Card', '2026-10-01T09:15:00');

IF NOT EXISTS (SELECT 1 FROM dbo.Payments WHERE Id = 2)
    INSERT INTO dbo.Payments (Id, AppointmentId, Amount, Method, PaidAt)
    VALUES (2, 1, 9300.00, N'BankTransfer', '2026-10-02T17:50:00');

IF NOT EXISTS (SELECT 1 FROM dbo.Payments WHERE Id = 3)
    INSERT INTO dbo.Payments (Id, AppointmentId, Amount, Method, PaidAt)
    VALUES (3, 2, 7000.00, N'Card', '2026-10-04T17:20:00');

IF NOT EXISTS (SELECT 1 FROM dbo.Payments WHERE Id = 4)
    INSERT INTO dbo.Payments (Id, AppointmentId, Amount, Method, PaidAt)
    VALUES (4, 3, 5000.00, N'Cash', '2026-10-10T09:40:00');

IF NOT EXISTS (SELECT 1 FROM dbo.Payments WHERE Id = 5)
    INSERT INTO dbo.Payments (Id, AppointmentId, Amount, Method, PaidAt)
    VALUES (5, 5, 3500.00, N'Card', '2026-10-12T08:45:00');

SET IDENTITY_INSERT dbo.Payments OFF;
PRINT N'Платежі Payments (5 записів) перевірені/додані.';
GO

/* -------------------------------------------------------------------------
   7. Історія статусів AppointmentStatusHistory
   ------------------------------------------------------------------------- */
SET IDENTITY_INSERT dbo.AppointmentStatusHistory ON;

-- Історія для Запису #1 (повний шлях до Delivered)
IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentStatusHistory WHERE Id = 1)
    INSERT INTO dbo.AppointmentStatusHistory (Id, AppointmentId, OldStatus, NewStatus, ChangedAt, ChangedBy)
    VALUES (1, 1, NULL, N'Booked', '2026-09-28T12:00:00', N'admin.iryna');
IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentStatusHistory WHERE Id = 2)
    INSERT INTO dbo.AppointmentStatusHistory (Id, AppointmentId, OldStatus, NewStatus, ChangedAt, ChangedBy)
    VALUES (2, 1, N'Booked', N'CarReceived', '2026-10-01T09:05:00', N'admin.iryna');
IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentStatusHistory WHERE Id = 3)
    INSERT INTO dbo.AppointmentStatusHistory (Id, AppointmentId, OldStatus, NewStatus, ChangedAt, ChangedBy)
    VALUES (3, 1, N'CarReceived', N'InProgress', '2026-10-01T09:30:00', N'master.andrii');
IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentStatusHistory WHERE Id = 4)
    INSERT INTO dbo.AppointmentStatusHistory (Id, AppointmentId, OldStatus, NewStatus, ChangedAt, ChangedBy)
    VALUES (4, 1, N'InProgress', N'QualityCheck', '2026-10-02T15:00:00', N'master.andrii');
IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentStatusHistory WHERE Id = 5)
    INSERT INTO dbo.AppointmentStatusHistory (Id, AppointmentId, OldStatus, NewStatus, ChangedAt, ChangedBy)
    VALUES (5, 1, N'QualityCheck', N'Ready', '2026-10-02T16:30:00', N'admin.iryna');
IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentStatusHistory WHERE Id = 6)
    INSERT INTO dbo.AppointmentStatusHistory (Id, AppointmentId, OldStatus, NewStatus, ChangedAt, ChangedBy)
    VALUES (6, 1, N'Ready', N'Delivered', '2026-10-02T18:00:00', N'admin.iryna');

-- Початкові записи історії для #2..#8
IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentStatusHistory WHERE Id = 7)
    INSERT INTO dbo.AppointmentStatusHistory (Id, AppointmentId, OldStatus, NewStatus, ChangedAt, ChangedBy)
    VALUES (7, 2, NULL, N'Booked', '2026-09-30T15:20:00', N'admin.iryna');
IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentStatusHistory WHERE Id = 8)
    INSERT INTO dbo.AppointmentStatusHistory (Id, AppointmentId, OldStatus, NewStatus, ChangedAt, ChangedBy)
    VALUES (8, 2, N'Ready', N'Delivered', '2026-10-04T17:30:00', N'admin.maksym');
IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentStatusHistory WHERE Id = 9)
    INSERT INTO dbo.AppointmentStatusHistory (Id, AppointmentId, OldStatus, NewStatus, ChangedAt, ChangedBy)
    VALUES (9, 3, NULL, N'Booked', '2026-10-07T11:00:00', N'admin.maksym');
IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentStatusHistory WHERE Id = 10)
    INSERT INTO dbo.AppointmentStatusHistory (Id, AppointmentId, OldStatus, NewStatus, ChangedAt, ChangedBy)
    VALUES (10, 3, N'QualityCheck', N'Ready', '2026-10-11T16:00:00', N'master.andrii');
IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentStatusHistory WHERE Id = 11)
    INSERT INTO dbo.AppointmentStatusHistory (Id, AppointmentId, OldStatus, NewStatus, ChangedAt, ChangedBy)
    VALUES (11, 5, N'CarReceived', N'InProgress', '2026-10-12T09:15:00', N'master.andrii');
IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentStatusHistory WHERE Id = 12)
    INSERT INTO dbo.AppointmentStatusHistory (Id, AppointmentId, OldStatus, NewStatus, ChangedAt, ChangedBy)
    VALUES (12, 8, N'Booked', N'Cancelled', '2026-10-05T18:00:00', N'admin.maksym');

SET IDENTITY_INSERT dbo.AppointmentStatusHistory OFF;
PRINT N'Історія статусів AppointmentStatusHistory перевірена/додана.';
PRINT N'=== Заповнення бази [DetailingBookings] успішно завершено ===';
GO
