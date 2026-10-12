/* =========================================================================
   Проєкт №1: Контекст "Записи" (Bookings)
   Файл: schema.sql
   База даних: DetailingBookings
   Майбутній стек: ADO.NET + Dapper
   Опис: Ідемпотентне створення таблиць, обмежень, табличного типу (TVP)
         та індексів для управління записами детейлінг-студії.
   ========================================================================= */

USE [master];
GO

IF DB_ID(N'DetailingBookings') IS NULL
BEGIN
    PRINT N'Створення бази даних [DetailingBookings]...';
    CREATE DATABASE [DetailingBookings];
    PRINT N'Базу даних [DetailingBookings] успішно створено.';
END
GO

USE [DetailingBookings];
GO

SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* -------------------------------------------------------------------------
   1. Таблиця Clients — клієнти детейлінг-студії (Корінь агрегата Client)
   Примітка: каскадне видалення записів при видаленні клієнта ЗАБОРОНЕНО.
   Використовується м'яке видалення (IsDeleted = 1).
   ------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.Clients', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Clients
    (
        Id          BIGINT          IDENTITY(1,1) NOT NULL,
        Email       NVARCHAR(255)   NOT NULL,
        FullName    NVARCHAR(200)   NOT NULL,
        Phone       NVARCHAR(30)    NOT NULL,
        -- Службові колонки аудиту та м'якого видалення
        CreatedAt   DATETIME2(3)    NOT NULL CONSTRAINT DF_Clients_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CreatedBy   NVARCHAR(100)   NOT NULL CONSTRAINT DF_Clients_CreatedBy DEFAULT (N'system'),
        UpdatedAt   DATETIME2(3)    NULL,
        UpdatedBy   NVARCHAR(100)   NULL,
        IsDeleted   BIT             NOT NULL CONSTRAINT DF_Clients_IsDeleted DEFAULT (0),

        CONSTRAINT PK_Clients PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Clients_Email UNIQUE (Email)
    );
    PRINT N'Таблицю [dbo.Clients] створено.';
END
GO

/* -------------------------------------------------------------------------
   2. Таблиця Services — локальна read-model копія довідника послуг з Каталогу
   ВАЖЛИВО: Поле Id БЕЗ IDENTITY! Значення Id надходить із бази DetailingCatalog
   через інтеграційні події. Міжбазового FOREIGN KEY немає і не може бути.
   ------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.Services', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Services
    (
        Id          BIGINT          NOT NULL, -- Логічне посилання на Service.Id у DetailingCatalog (БЕЗ IDENTITY)
        Name        NVARCHAR(200)   NOT NULL,
        Price       DECIMAL(18,2)   NOT NULL,
        DurationMin INT             NOT NULL,
        SyncedAt    DATETIME2(3)    NOT NULL CONSTRAINT DF_Services_SyncedAt DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT PK_Services PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_Services_Price CHECK (Price > 0),
        CONSTRAINT CK_Services_DurationMin CHECK (DurationMin > 0)
    );
    PRINT N'Таблицю [dbo.Services] створено.';
END
GO

/* -------------------------------------------------------------------------
   3. Таблиця Appointments — записи клієнтів на обслуговування (Корінь агрегата)
   Життєвий цикл статусів:
   Booked -> CarReceived -> InProgress -> QualityCheck -> Ready -> Delivered
   Cancelled дозволено лише з Booked та CarReceived.
   ------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.Appointments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Appointments
    (
        Id          BIGINT          IDENTITY(1,1) NOT NULL,
        ClientId    BIGINT          NOT NULL,
        Status      NVARCHAR(30)    NOT NULL CONSTRAINT DF_Appointments_Status DEFAULT (N'Booked'),
        ScheduledAt DATETIME2(3)    NOT NULL,
        TotalAmount DECIMAL(18,2)   NOT NULL CONSTRAINT DF_Appointments_TotalAmount DEFAULT (0.00),
        -- Службові колонки аудиту, м'якого видалення та оптимістичного блокування
        CreatedAt   DATETIME2(3)    NOT NULL CONSTRAINT DF_Appointments_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CreatedBy   NVARCHAR(100)   NOT NULL CONSTRAINT DF_Appointments_CreatedBy DEFAULT (N'system'),
        UpdatedAt   DATETIME2(3)    NULL,
        UpdatedBy   NVARCHAR(100)   NULL,
        IsDeleted   BIT             NOT NULL CONSTRAINT DF_Appointments_IsDeleted DEFAULT (0),
        RowVersion  ROWVERSION      NOT NULL,

        CONSTRAINT PK_Appointments PRIMARY KEY CLUSTERED (Id),
        -- Каскадне видалення для клієнтів НЕ дозволено (ON DELETE NO ACTION)
        CONSTRAINT FK_Appointments_Clients FOREIGN KEY (ClientId)
            REFERENCES dbo.Clients (Id) ON DELETE NO ACTION,
        CONSTRAINT CK_Appointments_Status CHECK (
            Status IN (
                N'Booked',
                N'CarReceived',
                N'InProgress',
                N'QualityCheck',
                N'Ready',
                N'Delivered',
                N'Cancelled'
            )
        ),
        CONSTRAINT CK_Appointments_TotalAmount CHECK (TotalAmount >= 0)
    );
    PRINT N'Таблицю [dbo.Appointments] створено.';
END
GO

/* -------------------------------------------------------------------------
   4. Таблиця AppointmentDetails — інформація про авто в межах запису (1:1)
   PK одночасно є FK на Appointments.Id з ON DELETE CASCADE (частина агрегата).
   ------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.AppointmentDetails', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppointmentDetails
    (
        AppointmentId BIGINT         NOT NULL,
        CarBrand      NVARCHAR(100)  NOT NULL,
        CarModel      NVARCHAR(100)  NOT NULL,
        PlateNumber   NVARCHAR(20)   NOT NULL,
        Comment       NVARCHAR(1000) NULL,

        CONSTRAINT PK_AppointmentDetails PRIMARY KEY CLUSTERED (AppointmentId),
        CONSTRAINT FK_AppointmentDetails_Appointments FOREIGN KEY (AppointmentId)
            REFERENCES dbo.Appointments (Id) ON DELETE CASCADE
    );
    PRINT N'Таблицю [dbo.AppointmentDetails] створено.';
END
GO

/* -------------------------------------------------------------------------
   5. Таблиця AppointmentItems — замовлені послуги в межах запису (M:N)
   Складений PK (AppointmentId, ServiceId).
   ServiceName та UnitPrice зберігаються як історичний знімок (Snapshot) на момент
   оформлення запису й ніколи не змінюються при подальшій зміні прайсу в Каталозі.
   FK на Appointments має ON DELETE CASCADE.
   ------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.AppointmentItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppointmentItems
    (
        AppointmentId BIGINT        NOT NULL,
        ServiceId     BIGINT        NOT NULL, -- Посилання на сутність іншого контексту — звичайний стовпець/поле з її Id, без зовнішнього ключа на іншу базу
        ServiceName   NVARCHAR(200) NOT NULL, -- Знімок назви послуги на момент запису
        UnitPrice     DECIMAL(18,2) NOT NULL, -- Знімок ціни за одиницю на момент запису
        Discount      DECIMAL(18,2) NOT NULL CONSTRAINT DF_AppointmentItems_Discount DEFAULT (0.00),
        Quantity      INT           NOT NULL CONSTRAINT DF_AppointmentItems_Quantity DEFAULT (1),

        CONSTRAINT PK_AppointmentItems PRIMARY KEY CLUSTERED (AppointmentId, ServiceId),
        CONSTRAINT FK_AppointmentItems_Appointments FOREIGN KEY (AppointmentId)
            REFERENCES dbo.Appointments (Id) ON DELETE CASCADE,
        CONSTRAINT FK_AppointmentItems_Services FOREIGN KEY (ServiceId)
            REFERENCES dbo.Services (Id) ON DELETE NO ACTION,
        CONSTRAINT CK_AppointmentItems_UnitPrice CHECK (UnitPrice > 0),
        CONSTRAINT CK_AppointmentItems_Discount CHECK (Discount >= 0),
        CONSTRAINT CK_AppointmentItems_Quantity CHECK (Quantity > 0),
        CONSTRAINT CK_AppointmentItems_DiscountLimit CHECK (Discount <= UnitPrice * Quantity)
    );
    PRINT N'Таблицю [dbo.AppointmentItems] створено.';
END
GO

/* -------------------------------------------------------------------------
   6. Таблиця Payments — платежі клієнта по запису (1:N)
   Фінансові транзакції не видаляються каскадно (ON DELETE NO ACTION).
   ------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.Payments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Payments
    (
        Id            BIGINT        IDENTITY(1,1) NOT NULL,
        AppointmentId BIGINT        NOT NULL,
        Amount        DECIMAL(18,2) NOT NULL,
        Method        NVARCHAR(30)  NOT NULL,
        PaidAt        DATETIME2(3)  NOT NULL CONSTRAINT DF_Payments_PaidAt DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT PK_Payments PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Payments_Appointments FOREIGN KEY (AppointmentId)
            REFERENCES dbo.Appointments (Id) ON DELETE NO ACTION,
        CONSTRAINT CK_Payments_Amount CHECK (Amount > 0),
        CONSTRAINT CK_Payments_Method CHECK (Method IN (N'Cash', N'Card', N'BankTransfer'))
    );
    PRINT N'Таблицю [dbo.Payments] створено.';
END
GO

/* -------------------------------------------------------------------------
   7. Таблиця AppointmentStatusHistory — журнал аудиту змін статусу запису (1:N)
   ------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.AppointmentStatusHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppointmentStatusHistory
    (
        Id            BIGINT        IDENTITY(1,1) NOT NULL,
        AppointmentId BIGINT        NOT NULL,
        OldStatus     NVARCHAR(30)  NULL,
        NewStatus     NVARCHAR(30)  NOT NULL,
        ChangedAt     DATETIME2(3)  NOT NULL CONSTRAINT DF_AppointmentStatusHistory_ChangedAt DEFAULT (SYSUTCDATETIME()),
        ChangedBy     NVARCHAR(100) NOT NULL CONSTRAINT DF_AppointmentStatusHistory_ChangedBy DEFAULT (N'system'),

        CONSTRAINT PK_AppointmentStatusHistory PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_AppointmentStatusHistory_Appointments FOREIGN KEY (AppointmentId)
            REFERENCES dbo.Appointments (Id) ON DELETE NO ACTION
    );
    PRINT N'Таблицю [dbo.AppointmentStatusHistory] створено.';
END
GO

/* -------------------------------------------------------------------------
   8. Табличний тип (TVP) для передачі позицій замовлення у usp_CreateAppointment
   ------------------------------------------------------------------------- */
IF TYPE_ID(N'dbo.AppointmentItemTableType') IS NULL
BEGIN
    CREATE TYPE dbo.AppointmentItemTableType AS TABLE
    (
        ServiceId BIGINT        NOT NULL,
        Quantity  INT           NOT NULL DEFAULT (1) CHECK (Quantity > 0),
        Discount  DECIMAL(18,2) NOT NULL DEFAULT (0.00) CHECK (Discount >= 0),
        PRIMARY KEY (ServiceId)
    );
    PRINT N'Табличний тип [dbo.AppointmentItemTableType] створено.';
END
GO

/* =========================================================================
   ІНДЕКСИ (з коментарями, під який саме запит створено кожен індекс)
   ========================================================================= */

-- 1. Індекс під запит вибірки записів конкретного клієнта з фільтрацією за певним статусом
-- Запит: SELECT ... FROM dbo.Appointments WHERE ClientId = @ClientId AND Status = @Status ORDER BY ScheduledAt DESC;
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Appointments_ClientId_Status'
      AND object_id = OBJECT_ID(N'dbo.Appointments')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Appointments_ClientId_Status
        ON dbo.Appointments (ClientId, Status)
        INCLUDE (ScheduledAt, TotalAmount, IsDeleted);
    PRINT N'Індекс [IX_Appointments_ClientId_Status] створено.';
END
GO

-- 2. Індекс під запит формування черги автомобілів на сьогодні (або на вибраний діапазон дат) за статусом
-- Запит: SELECT ... FROM dbo.Appointments WHERE Status = @Status AND ScheduledAt >= @StartOfDay AND ScheduledAt < @EndOfDay;
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Appointments_Status_ScheduledAt'
      AND object_id = OBJECT_ID(N'dbo.Appointments')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Appointments_Status_ScheduledAt
        ON dbo.Appointments (Status, ScheduledAt)
        INCLUDE (ClientId, TotalAmount);
    PRINT N'Індекс [IX_Appointments_Status_ScheduledAt] створено.';
END
GO

-- 3. Індекс під запит швидкого підрахунку суми оплат по конкретному запису (у usp_RegisterPayment та usp_DeliverCar)
-- Запит: SELECT ISNULL(SUM(Amount), 0) FROM dbo.Payments WHERE AppointmentId = @AppointmentId;
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Payments_AppointmentId'
      AND object_id = OBJECT_ID(N'dbo.Payments')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Payments_AppointmentId
        ON dbo.Payments (AppointmentId)
        INCLUDE (Amount, Method, PaidAt);
    PRINT N'Індекс [IX_Payments_AppointmentId] створено.';
END
GO

-- 4. Фільтрований індекс лише для "живих" (не видалених м'яко) записів клієнта (використовується в usp_GetClientAppointments)
-- Запит: SELECT ... FROM dbo.Appointments WHERE ClientId = @ClientId AND IsDeleted = 0 ORDER BY ScheduledAt DESC;
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Appointments_ClientId_Active'
      AND object_id = OBJECT_ID(N'dbo.Appointments')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Appointments_ClientId_Active
        ON dbo.Appointments (ClientId, ScheduledAt DESC)
        INCLUDE (Status, TotalAmount)
        WHERE IsDeleted = 0;
    PRINT N'Фільтрований індекс [IX_Appointments_ClientId_Active] створено.';
END
GO
