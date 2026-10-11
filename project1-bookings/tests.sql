/* =========================================================================
   Проєкт №1: Контекст "Записи" (Bookings)
   Файл: tests.sql
   База даних: DetailingBookings
   Опис: Набір демонстраційних та перевірочних сценаріїв:
         1. Прості CRUD-операції (INSERT / SELECT / UPDATE / soft DELETE)
            для Clients та Appointments (включно з перевіркою RowVersion).
         2. Позитивний сценарій повного життєвого циклу запису (від створення
            через TVP до видачі авто клієнту та перевірки незмінності знімка).
         3. Негативні сценарії в TRY/CATCH з виводом очікуваних помилок
            (InProgress -> Delivered, скасування після початку робіт,
            видача без повної оплати, переплата понад TotalAmount).
         4. Перевірка пагінації (usp_GetClientAppointments).
         5. Аудит зовнішніх ключів (SELECT * FROM sys.foreign_keys).
   ========================================================================= */

USE [DetailingBookings];
GO

SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

PRINT N'=========================================================================';
PRINT N'ЧАСТИНА 1. ПРОСТІ CRUD-ЗАПИТИ ДЛЯ CLIENTS ТА APPOINTMENTS (SOFT DELETE)';
PRINT N'=========================================================================';

-- 1.1. CREATE (INSERT): Додавання тестового клієнта (ідемпотентно)
DECLARE @CrudClientId BIGINT;

IF NOT EXISTS (SELECT 1 FROM dbo.Clients WHERE Email = N'crud.test.client@example.ua')
BEGIN
    INSERT INTO dbo.Clients (Email, FullName, Phone, CreatedBy)
    VALUES (N'crud.test.client@example.ua', N'Ігор Савченко (Тест CRUD)', N'+380661239876', N'test.runner');
END

SELECT @CrudClientId = Id FROM dbo.Clients WHERE Email = N'crud.test.client@example.ua';
PRINT FORMATMESSAGE(N'[CRUD] Тестовий клієнт має Id = %I64d', @CrudClientId);

-- 1.2. READ (SELECT): Вибірка клієнта
SELECT Id, Email, FullName, Phone, IsDeleted, CreatedAt, UpdatedAt
FROM dbo.Clients
WHERE Id = @CrudClientId;

-- 1.3. UPDATE: Оновлення контактного номера телефону та імені
UPDATE dbo.Clients
SET Phone     = N'+380669998877',
    UpdatedAt = SYSUTCDATETIME(),
    UpdatedBy = N'admin.iryna'
WHERE Id = @CrudClientId
  AND IsDeleted = 0;

-- 1.4. CREATE & UPDATE з оптимістичним блокуванням (RowVersion) для Appointments
DECLARE @CrudAppointmentId BIGINT;
DECLARE @CurrentRowVersion BINARY(8);

INSERT INTO dbo.Appointments (ClientId, Status, ScheduledAt, TotalAmount, CreatedBy)
VALUES (@CrudClientId, N'Booked', DATEADD(DAY, 5, SYSUTCDATETIME()), 800.00, N'test.runner');

SET @CrudAppointmentId = SCOPE_IDENTITY();

-- Зчитуємо поточний RowVersion перед редагуванням (як це робить Dapper у репозиторії)
SELECT @CurrentRowVersion = RowVersion
FROM dbo.Appointments
WHERE Id = @CrudAppointmentId;

-- Оновлюємо час запису з перевіркою RowVersion
UPDATE dbo.Appointments
SET ScheduledAt = DATEADD(HOUR, 2, ScheduledAt),
    UpdatedAt   = SYSUTCDATETIME(),
    UpdatedBy   = N'admin.iryna'
WHERE Id = @CrudAppointmentId
  AND RowVersion = @CurrentRowVersion;

PRINT FORMATMESSAGE(N'[CRUD] Запис #%I64d успішно оновлено з перевіркою RowVersion (оновлено рядків: %d).', @CrudAppointmentId, @@ROWCOUNT);

-- 1.5. SOFT DELETE: М'яке видалення запису та клієнта (фізично рядки залишаються в БД)
UPDATE dbo.Appointments
SET IsDeleted = 1,
    UpdatedAt = SYSUTCDATETIME(),
    UpdatedBy = N'test.runner'
WHERE Id = @CrudAppointmentId;

UPDATE dbo.Clients
SET IsDeleted = 1,
    UpdatedAt = SYSUTCDATETIME(),
    UpdatedBy = N'test.runner'
WHERE Id = @CrudClientId;

-- Переконуємося, що usp_GetClientAppointments ігнорує м'яко видалені записи (IsDeleted = 1)
PRINT N'[CRUD] Перевірка вибірки активних записів після Soft Delete (очікується 0 рядків):';
EXEC dbo.usp_GetClientAppointments @ClientId = @CrudClientId;

-- Прибираємо тестовий запис та тестового клієнта після демонстрації CRUD/Soft Delete, щоб зберегти еталонний стан БД
DELETE FROM dbo.Appointments WHERE Id = @CrudAppointmentId;
DELETE FROM dbo.Clients WHERE Id = @CrudClientId;
GO

PRINT N'';
PRINT N'=========================================================================';
PRINT N'ЧАСТИНА 2. ПОЗИТИВНИЙ СЦЕНАРІЙ ПОВНОГО ЖИТТЄВОГО ЦИКЛУ ЗАПИСУ';
PRINT N'=========================================================================';

DECLARE @LifecycleAppointmentId BIGINT;
DECLARE @OrderItems dbo.AppointmentItemTableType;

-- Ідемпотентне очищення попереднього тестового запису (якщо tests.sql запускається повторно)
DELETE p FROM dbo.Payments p INNER JOIN dbo.AppointmentDetails d ON d.AppointmentId = p.AppointmentId WHERE d.PlateNumber = N'AA8899KX';
DELETE h FROM dbo.AppointmentStatusHistory h INNER JOIN dbo.AppointmentDetails d ON d.AppointmentId = h.AppointmentId WHERE d.PlateNumber = N'AA8899KX';
DELETE a FROM dbo.Appointments a INNER JOIN dbo.AppointmentDetails d ON d.AppointmentId = a.Id WHERE d.PlateNumber = N'AA8899KX';

-- Обираємо 2 послуги: Трифазна нано-мийка (Id=2, 1200 грн) + Антидощ (Id=8, 1800 грн, знижка 100 грн)
-- Очікуваний TotalAmount = 1200 + (1800 - 100) = 2900.00 грн
INSERT INTO @OrderItems (ServiceId, Quantity, Discount)
VALUES
    (2, 1, 0.00),
    (8, 1, 100.00);

-- Крок 1: Створення запису через usp_CreateAppointment
EXEC dbo.usp_CreateAppointment
    @ClientId         = 1,
    @ScheduledAt      = '2026-10-20T10:00:00',
    @CarBrand         = N'Lexus',
    @CarModel         = N'RX 350',
    @PlateNumber      = N'AA8899KX',
    @Comment          = N'Повний тест життєвого циклу',
    @CreatedBy        = N'admin.iryna',
    @Items            = @OrderItems,
    @NewAppointmentId = @LifecycleAppointmentId OUTPUT;

PRINT FORMATMESSAGE(N'[Позитивний тест] Створено новий запис #%I64d у статусі Booked.', @LifecycleAppointmentId);

-- Крок 2: Демонстрація політики знімків (Snapshot):
-- Якщо ціна послуги #8 у локальному довіднику Services тимчасово зміниться,
-- знімок UnitPrice у створеному записі залишиться незмінним (1800.00 грн).
UPDATE dbo.Services SET Price = 2100.00 WHERE Id = 8;

SELECT
    ai.AppointmentId,
    ai.ServiceId,
    ai.ServiceName       AS SnapshotServiceName,
    ai.UnitPrice         AS SnapshotUnitPrice,
    s.Price              AS CurrentCatalogReplicaPrice
FROM dbo.AppointmentItems ai
INNER JOIN dbo.Services s ON s.Id = ai.ServiceId
WHERE ai.AppointmentId = @LifecycleAppointmentId;

-- Повертаємо еталонну ціну послуги #8 назад до 1800.00
UPDATE dbo.Services SET Price = 1800.00 WHERE Id = 8;

-- Крок 3: Послідовні допустимі переходи статусів:
-- Booked -> CarReceived -> InProgress -> QualityCheck -> Ready
EXEC dbo.usp_ChangeStatus @AppointmentId = @LifecycleAppointmentId, @NewStatus = N'CarReceived',  @ChangedBy = N'admin.iryna';
EXEC dbo.usp_ChangeStatus @AppointmentId = @LifecycleAppointmentId, @NewStatus = N'InProgress',   @ChangedBy = N'master.andrii';
EXEC dbo.usp_ChangeStatus @AppointmentId = @LifecycleAppointmentId, @NewStatus = N'QualityCheck', @ChangedBy = N'master.andrii';
EXEC dbo.usp_ChangeStatus @AppointmentId = @LifecycleAppointmentId, @NewStatus = N'Ready',        @ChangedBy = N'admin.iryna';
PRINT FORMATMESSAGE(N'[Позитивний тест] Запис #%I64d успішно переведено через усі етапи до статусу Ready.', @LifecycleAppointmentId);

-- Крок 4: Оплата двома частинами (1000.00 готівкою + 1900.00 карткою = 2900.00)
EXEC dbo.usp_RegisterPayment @AppointmentId = @LifecycleAppointmentId, @Amount = 1000.00, @Method = N'Cash';
EXEC dbo.usp_RegisterPayment @AppointmentId = @LifecycleAppointmentId, @Amount = 1900.00, @Method = N'Card';
PRINT FORMATMESSAGE(N'[Позитивний тест] Зареєстровано повну оплату 2900.00 грн для запису #%I64d.', @LifecycleAppointmentId);

-- Крок 5: Видача авто клієнту (Ready -> Delivered за умови повної оплати)
EXEC dbo.usp_DeliverCar @AppointmentId = @LifecycleAppointmentId, @ChangedBy = N'admin.iryna';
PRINT FORMATMESSAGE(N'[Позитивний тест] Авто за записом #%I64d успішно видано клієнту (Status = Delivered).', @LifecycleAppointmentId);

-- Перевірка повної історії переходів створеного запису
SELECT
    Id,
    AppointmentId,
    OldStatus,
    NewStatus,
    ChangedAt,
    ChangedBy
FROM dbo.AppointmentStatusHistory
WHERE AppointmentId = @LifecycleAppointmentId
ORDER BY Id ASC;
GO

PRINT N'';
PRINT N'=========================================================================';
PRINT N'ЧАСТИНА 3. НЕГАТИВНІ СЦЕНАРІЇ (НАВМИСНО НЕДОПУСТИМІ ПЕРЕХОДИ В TRY/CATCH)';
PRINT N'=========================================================================';

-- 3.1. Спроба недопустимого стрибка статусу: InProgress -> Delivered (Запис #5 перебуває в InProgress)
BEGIN TRY
    PRINT N'[Негативний тест 3.1] Спроба перевести запис #5 напряму з [InProgress] у [Delivered]...';
    EXEC dbo.usp_ChangeStatus
        @AppointmentId = 5,
        @NewStatus     = N'Delivered',
        @ChangedBy     = N'admin.maksym';
    PRINT N'ПОМИЛКА ТЕСТУ: Перехід не мав бути дозволений!';
END TRY
BEGIN CATCH
    PRINT FORMATMESSAGE(
        N'[ОЧІКУВАНА ПОМИЛКА ПЕРЕХОПЛЕНА] Код: %d | Повідомлення: %s',
        ERROR_NUMBER(),
        ERROR_MESSAGE()
    );
END CATCH;

-- 3.2. Спроба скасування запису після початку робіт: Запис #5 має статус InProgress
BEGIN TRY
    PRINT N'[Негативний тест 3.2] Спроба скасувати запис #5, який вже перебуває в статусі [InProgress]...';
    EXEC dbo.usp_CancelAppointment
        @AppointmentId = 5,
        @ChangedBy     = N'admin.maksym';
    PRINT N'ПОМИЛКА ТЕСТУ: Скасування після початку робіт не мало пройти!';
END TRY
BEGIN CATCH
    PRINT FORMATMESSAGE(
        N'[ОЧІКУВАНА ПОМИЛКА ПЕРЕХОПЛЕНА] Код: %d | Повідомлення: %s',
        ERROR_NUMBER(),
        ERROR_MESSAGE()
    );
END CATCH;

-- 3.3. Спроба видачі автомобіля без повної оплати:
-- Запис #3 має статус Ready, але сплачено лише 5000.00 грн із 11500.00 грн
BEGIN TRY
    PRINT N'[Негативний тест 3.3] Спроба видати авто за записом #3 (Ready) при частковій оплаті (5000 із 11500)...';
    EXEC dbo.usp_DeliverCar
        @AppointmentId = 3,
        @ChangedBy     = N'admin.maksym';
    PRINT N'ПОМИЛКА ТЕСТУ: Видача авто без повної оплати не мала пройти!';
END TRY
BEGIN CATCH
    PRINT FORMATMESSAGE(
        N'[ОЧІКУВАНА ПОМИЛКА ПЕРЕХОПЛЕНА] Код: %d | Повідомлення: %s',
        ERROR_NUMBER(),
        ERROR_MESSAGE()
    );
END CATCH;

-- 3.4. Спроба зареєструвати платіж, що перевищує TotalAmount:
-- Запис #3 має залишок до сплати 6500.00 грн (11500 - 5000), пробуємо внести 8000.00 грн
BEGIN TRY
    PRINT N'[Негативний тест 3.4] Спроба внести платіж 8000.00 грн при залишку 6500.00 грн для запису #3...';
    EXEC dbo.usp_RegisterPayment
        @AppointmentId = 3,
        @Amount        = 8000.00,
        @Method        = N'Card';
    PRINT N'ПОМИЛКА ТЕСТУ: Переплата понад TotalAmount не мала пройти!';
END TRY
BEGIN CATCH
    PRINT FORMATMESSAGE(
        N'[ОЧІКУВАНА ПОМИЛКА ПЕРЕХОПЛЕНА] Код: %d | Повідомлення: %s',
        ERROR_NUMBER(),
        ERROR_MESSAGE()
    );
END CATCH;
GO

PRINT N'';
PRINT N'=========================================================================';
PRINT N'ЧАСТИНА 4. ПЕРЕВІРКА ПАГІНАЦІЇ ТА ФІЛЬТРАЦІЇ (usp_GetClientAppointments)';
PRINT N'=========================================================================';

-- Сторінка 1 (по 2 записи на сторінку) для клієнта #1 (Олександр Коваленко)
PRINT N'[Пагінація] Клієнт #1 — Сторінка 1 (PageSize = 2):';
EXEC dbo.usp_GetClientAppointments
    @ClientId   = 1,
    @Status     = NULL,
    @PageNumber = 1,
    @PageSize   = 2;

-- Сторінка 2 (по 2 записи на сторінку) для клієнта #1
PRINT N'[Пагінація] Клієнт #1 — Сторінка 2 (PageSize = 2):';
EXEC dbo.usp_GetClientAppointments
    @ClientId   = 1,
    @Status     = NULL,
    @PageNumber = 2,
    @PageSize   = 2;

-- Фільтрація лише за статусом Delivered для клієнта #1
PRINT N'[Фільтрація] Клієнт #1 — Лише записи у статусі Delivered:';
EXEC dbo.usp_GetClientAppointments
    @ClientId   = 1,
    @Status     = N'Delivered',
    @PageNumber = 1,
    @PageSize   = 10;
GO

PRINT N'';
PRINT N'=========================================================================';
PRINT N'ЧАСТИНА 5. ПЕРЕВІРКА ЗОВНІШНІХ КЛЮЧІВ (ВІДСУТНІСТЬ МІЖБАЗОВИХ FK)';
PRINT N'=========================================================================';

SELECT
    fk.name                                     AS ForeignKeyName,
    OBJECT_SCHEMA_NAME(fk.parent_object_id)     AS ParentSchema,
    OBJECT_NAME(fk.parent_object_id)            AS ParentTable,
    OBJECT_SCHEMA_NAME(fk.referenced_object_id) AS ReferencedSchema,
    OBJECT_NAME(fk.referenced_object_id)        AS ReferencedTable,
    fk.delete_referential_action_desc           AS OnDeleteAction
FROM sys.foreign_keys fk
ORDER BY ParentTable, ForeignKeyName;
GO
