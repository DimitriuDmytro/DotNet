/* =========================================================================
   Проєкт №1: Контекст "Записи" (Bookings)
   Файл: procedures.sql
   База даних: DetailingBookings
   Опис: 6 збережених процедур для керування життєвим циклом записів,
         фіксації знімків цін, реєстрації оплат та пагінованої вибірки.
   ========================================================================= */

USE [DetailingBookings];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- Переконуємося, що табличний тип існує перед створенням процедур
IF TYPE_ID(N'dbo.AppointmentItemTableType') IS NULL
BEGIN
    CREATE TYPE dbo.AppointmentItemTableType AS TABLE
    (
        ServiceId BIGINT        NOT NULL,
        Quantity  INT           NOT NULL DEFAULT (1) CHECK (Quantity > 0),
        Discount  DECIMAL(18,2) NOT NULL DEFAULT (0.00) CHECK (Discount >= 0),
        PRIMARY KEY (ServiceId)
    );
END
GO

/* -------------------------------------------------------------------------
   1. usp_CreateAppointment
   Призначення: Створює новий запис (Appointments), деталі авто (AppointmentDetails),
                позиції замовлення (AppointmentItems) через TVP зі знімком ServiceName
                та UnitPrice із локальної таблиці Services, обчислює TotalAmount
                і додає початковий запис у AppointmentStatusHistory.
   ------------------------------------------------------------------------- */
CREATE OR ALTER PROCEDURE dbo.usp_CreateAppointment
    @ClientId         BIGINT,
    @ScheduledAt      DATETIME2(3),
    @CarBrand         NVARCHAR(100),
    @CarModel         NVARCHAR(100),
    @PlateNumber      NVARCHAR(20),
    @Comment          NVARCHAR(1000) = NULL,
    @CreatedBy        NVARCHAR(100)  = N'system',
    @Items            dbo.AppointmentItemTableType READONLY,
    @NewAppointmentId BIGINT         = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        -- Перевірка існування активного клієнта
        IF NOT EXISTS (SELECT 1 FROM dbo.Clients WHERE Id = @ClientId AND IsDeleted = 0)
        BEGIN
            ;THROW 50001, N'Клієнта з вказаним ідентифікатором не знайдено або його видалено.', 1;
        END

        -- Перевірка наявності хоча б однієї послуги у замовленні
        IF NOT EXISTS (SELECT 1 FROM @Items)
        BEGIN
            ;THROW 50005, N'Запис повинен містити щонайменше одну послугу.', 1;
        END

        -- Перевірка, що всі передані ServiceId існують у локальній таблиці Services
        IF EXISTS (
            SELECT 1
            FROM @Items i
            LEFT JOIN dbo.Services s ON s.Id = i.ServiceId
            WHERE s.Id IS NULL
        )
        BEGIN
            ;THROW 50005, N'Одна або декілька вказаних послуг відсутні в локальному довіднику Services.', 1;
        END

        -- Перевірка, що знижка не перевищує вартість позиції
        IF EXISTS (
            SELECT 1
            FROM @Items i
            INNER JOIN dbo.Services s ON s.Id = i.ServiceId
            WHERE i.Discount > (s.Price * i.Quantity)
        )
        BEGIN
            ;THROW 50005, N'Сума знижки по позиції не може перевищувати вартість послуги.', 1;
        END

        DECLARE @CalculatedTotal DECIMAL(18,2);
        SELECT @CalculatedTotal = SUM((s.Price * i.Quantity) - i.Discount)
        FROM @Items i
        INNER JOIN dbo.Services s ON s.Id = i.ServiceId;

        BEGIN TRANSACTION;

        -- 1. Створення кореневого запису Appointment
        INSERT INTO dbo.Appointments
        (
            ClientId,
            Status,
            ScheduledAt,
            TotalAmount,
            CreatedAt,
            CreatedBy
        )
        VALUES
        (
            @ClientId,
            N'Booked',
            @ScheduledAt,
            @CalculatedTotal,
            SYSUTCDATETIME(),
            @CreatedBy
        );

        SET @NewAppointmentId = SCOPE_IDENTITY();

        -- 2. Створення деталей автомобіля (1:1)
        INSERT INTO dbo.AppointmentDetails
        (
            AppointmentId,
            CarBrand,
            CarModel,
            PlateNumber,
            Comment
        )
        VALUES
        (
            @NewAppointmentId,
            @CarBrand,
            @CarModel,
            @PlateNumber,
            @Comment
        );

        -- 3. Створення позицій запису зі знімком (Snapshot) ServiceName та UnitPrice
        INSERT INTO dbo.AppointmentItems
        (
            AppointmentId,
            ServiceId,
            ServiceName,
            UnitPrice,
            Discount,
            Quantity
        )
        SELECT
            @NewAppointmentId,
            i.ServiceId,
            s.Name,   -- Знімок назви послуги на момент запису
            s.Price,  -- Знімок ціни послуги на момент запису
            i.Discount,
            i.Quantity
        FROM @Items i
        INNER JOIN dbo.Services s ON s.Id = i.ServiceId;

        -- 4. Запис початкового стану в історію статусів
        INSERT INTO dbo.AppointmentStatusHistory
        (
            AppointmentId,
            OldStatus,
            NewStatus,
            ChangedAt,
            ChangedBy
        )
        VALUES
        (
            @NewAppointmentId,
            NULL,
            N'Booked',
            SYSUTCDATETIME(),
            @CreatedBy
        );

        COMMIT TRANSACTION;

        SELECT
            @NewAppointmentId AS AppointmentId,
            @CalculatedTotal  AS TotalAmount,
            N'Booked'         AS Status;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        ;THROW;
    END CATCH
END
GO

/* -------------------------------------------------------------------------
   2. usp_ChangeStatus
   Призначення: Атомарна зміна статусу запису з блокуванням рядка (UPDLOCK),
                перевіркою за таблицею допустимих переходів та аудитом.
                Коди помилок:
                50001 — запис не знайдено;
                50002 — недопустимий перехід між статусами.
   ------------------------------------------------------------------------- */
CREATE OR ALTER PROCEDURE dbo.usp_ChangeStatus
    @AppointmentId BIGINT,
    @NewStatus     NVARCHAR(30),
    @ChangedBy     NVARCHAR(100) = N'system'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -- Таблиця дозволених переходів між статусами
    DECLARE @AllowedTransitions TABLE
    (
        FromStatus NVARCHAR(30) NOT NULL,
        ToStatus   NVARCHAR(30) NOT NULL,
        PRIMARY KEY (FromStatus, ToStatus)
    );

    INSERT INTO @AllowedTransitions (FromStatus, ToStatus)
    VALUES
        (N'Booked',       N'CarReceived'),
        (N'CarReceived',  N'InProgress'),
        (N'InProgress',   N'QualityCheck'),
        (N'QualityCheck', N'Ready'),
        (N'Ready',        N'Delivered'),
        (N'Booked',       N'Cancelled'),
        (N'CarReceived',  N'Cancelled');

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @CurrentStatus NVARCHAR(30);

        -- Читаємо поточний статус з ексклюзивним наміром оновлення (UPDLOCK, ROWLOCK)
        SELECT @CurrentStatus = Status
        FROM dbo.Appointments WITH (UPDLOCK, ROWLOCK)
        WHERE Id = @AppointmentId
          AND IsDeleted = 0;

        IF @CurrentStatus IS NULL
        BEGIN
            ;THROW 50001, N'Запис не знайдено або він позначений як видалений.', 1;
        END

        -- Перевірка допустимості переходу за таблицею переходів
        IF NOT EXISTS (
            SELECT 1
            FROM @AllowedTransitions
            WHERE FromStatus = @CurrentStatus
              AND ToStatus = @NewStatus
        )
        BEGIN
            DECLARE @ErrMsg NVARCHAR(400) = FORMATMESSAGE(
                N'Недопустимий перехід статусу для запису #%I64d: з [%s] у [%s].',
                @AppointmentId, @CurrentStatus, @NewStatus
            );
            ;THROW 50002, @ErrMsg, 1;
        END

        DECLARE @Now DATETIME2(3) = SYSUTCDATETIME();

        UPDATE dbo.Appointments
        SET Status    = @NewStatus,
            UpdatedAt = @Now,
            UpdatedBy = @ChangedBy
        WHERE Id = @AppointmentId;

        INSERT INTO dbo.AppointmentStatusHistory
        (
            AppointmentId,
            OldStatus,
            NewStatus,
            ChangedAt,
            ChangedBy
        )
        VALUES
        (
            @AppointmentId,
            @CurrentStatus,
            @NewStatus,
            @Now,
            @ChangedBy
        );

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        ;THROW;
    END CATCH
END
GO

/* -------------------------------------------------------------------------
   3. usp_RegisterPayment
   Призначення: Додає платіж у транзакції; перевіряє, щоб загальна сума
                платежів по запису не перевищувала TotalAmount.
   ------------------------------------------------------------------------- */
CREATE OR ALTER PROCEDURE dbo.usp_RegisterPayment
    @AppointmentId BIGINT,
    @Amount        DECIMAL(18,2),
    @Method        NVARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        IF @Amount <= 0
        BEGIN
            ;THROW 50003, N'Сума платежу повинна бути більшою за нуль.', 1;
        END

        BEGIN TRANSACTION;

        DECLARE @TotalAmount   DECIMAL(18,2);
        DECLARE @CurrentStatus NVARCHAR(30);

        -- Блокуємо запис на час перевірки балансу оплат
        SELECT
            @TotalAmount   = TotalAmount,
            @CurrentStatus = Status
        FROM dbo.Appointments WITH (UPDLOCK, ROWLOCK)
        WHERE Id = @AppointmentId
          AND IsDeleted = 0;

        IF @CurrentStatus IS NULL
        BEGIN
            ;THROW 50001, N'Запис для реєстрації оплати не знайдено.', 1;
        END

        IF @CurrentStatus = N'Cancelled'
        BEGIN
            ;THROW 50003, N'Неможливо зареєструвати оплату для скасованого запису.', 1;
        END

        DECLARE @AlreadyPaid DECIMAL(18,2);
        SELECT @AlreadyPaid = ISNULL(SUM(Amount), 0.00)
        FROM dbo.Payments
        WHERE AppointmentId = @AppointmentId;

        IF (@AlreadyPaid + @Amount) > @TotalAmount
        BEGIN
            DECLARE @OverpayMsg NVARCHAR(400) = FORMATMESSAGE(
                N'Сума платежу перевищує залишок до сплати. TotalAmount: %s, вже сплачено: %s, спроба оплати: %s.',
                CAST(@TotalAmount AS NVARCHAR(30)),
                CAST(@AlreadyPaid AS NVARCHAR(30)),
                CAST(@Amount AS NVARCHAR(30))
            );
            ;THROW 50003, @OverpayMsg, 1;
        END

        INSERT INTO dbo.Payments
        (
            AppointmentId,
            Amount,
            Method,
            PaidAt
        )
        VALUES
        (
            @AppointmentId,
            @Amount,
            @Method,
            SYSUTCDATETIME()
        );

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        ;THROW;
    END CATCH
END
GO

/* -------------------------------------------------------------------------
   4. usp_DeliverCar
   Призначення: Видача автомобіля клієнту. Відмовляє (THROW), якщо поточний
                статус не Ready (50002) або сума оплат менша за TotalAmount (50004).
   ------------------------------------------------------------------------- */
CREATE OR ALTER PROCEDURE dbo.usp_DeliverCar
    @AppointmentId BIGINT,
    @ChangedBy     NVARCHAR(100) = N'system'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @CurrentStatus NVARCHAR(30);
        DECLARE @TotalAmount   DECIMAL(18,2);

        SELECT
            @CurrentStatus = Status,
            @TotalAmount   = TotalAmount
        FROM dbo.Appointments WITH (UPDLOCK, ROWLOCK)
        WHERE Id = @AppointmentId
          AND IsDeleted = 0;

        IF @CurrentStatus IS NULL
        BEGIN
            ;THROW 50001, N'Запис не знайдено або він видалений.', 1;
        END

        IF @CurrentStatus <> N'Ready'
        BEGIN
            DECLARE @StatusMsg NVARCHAR(400) = FORMATMESSAGE(
                N'Видача авто неможлива: поточний статус запису #%I64d є [%s], очікується [Ready].',
                @AppointmentId, @CurrentStatus
            );
            ;THROW 50002, @StatusMsg, 1;
        END

        DECLARE @TotalPaid DECIMAL(18,2);
        SELECT @TotalPaid = ISNULL(SUM(Amount), 0.00)
        FROM dbo.Payments
        WHERE AppointmentId = @AppointmentId;

        IF @TotalPaid < @TotalAmount
        BEGIN
            DECLARE @DebtMsg NVARCHAR(400) = FORMATMESSAGE(
                N'Видача авто заборонена: запис #%I64d оплачено не повністю (сплачено %s із %s).',
                @AppointmentId,
                CAST(@TotalPaid AS NVARCHAR(30)),
                CAST(@TotalAmount AS NVARCHAR(30))
            );
            ;THROW 50004, @DebtMsg, 1;
        END

        DECLARE @Now DATETIME2(3) = SYSUTCDATETIME();

        UPDATE dbo.Appointments
        SET Status    = N'Delivered',
            UpdatedAt = @Now,
            UpdatedBy = @ChangedBy
        WHERE Id = @AppointmentId;

        INSERT INTO dbo.AppointmentStatusHistory
        (
            AppointmentId,
            OldStatus,
            NewStatus,
            ChangedAt,
            ChangedBy
        )
        VALUES
        (
            @AppointmentId,
            @CurrentStatus,
            N'Delivered',
            @Now,
            @ChangedBy
        );

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        ;THROW;
    END CATCH
END
GO

/* -------------------------------------------------------------------------
   5. usp_CancelAppointment
   Призначення: Скасування запису. Дозволено виключно зі статусів
                Booked або CarReceived (до початку фактичних робіт).
   ------------------------------------------------------------------------- */
CREATE OR ALTER PROCEDURE dbo.usp_CancelAppointment
    @AppointmentId BIGINT,
    @ChangedBy     NVARCHAR(100) = N'system'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @CurrentStatus NVARCHAR(30);

        SELECT @CurrentStatus = Status
        FROM dbo.Appointments WITH (UPDLOCK, ROWLOCK)
        WHERE Id = @AppointmentId
          AND IsDeleted = 0;

        IF @CurrentStatus IS NULL
        BEGIN
            ;THROW 50001, N'Запис не знайдено або він видалений.', 1;
        END

        IF @CurrentStatus NOT IN (N'Booked', N'CarReceived')
        BEGIN
            DECLARE @CancelErr NVARCHAR(400) = FORMATMESSAGE(
                N'Скасування запису #%I64d заборонено: поточний статус [%s]. Скасування можливе лише зі статусів [Booked] або [CarReceived].',
                @AppointmentId, @CurrentStatus
            );
            ;THROW 50002, @CancelErr, 1;
        END

        DECLARE @Now DATETIME2(3) = SYSUTCDATETIME();

        UPDATE dbo.Appointments
        SET Status    = N'Cancelled',
            UpdatedAt = @Now,
            UpdatedBy = @ChangedBy
        WHERE Id = @AppointmentId;

        INSERT INTO dbo.AppointmentStatusHistory
        (
            AppointmentId,
            OldStatus,
            NewStatus,
            ChangedAt,
            ChangedBy
        )
        VALUES
        (
            @AppointmentId,
            @CurrentStatus,
            N'Cancelled',
            @Now,
            @ChangedBy
        );

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        ;THROW;
    END CATCH
END
GO

/* -------------------------------------------------------------------------
   6. usp_GetClientAppointments
   Призначення: Вибірка записів клієнта з опціональним фільтром за статусом,
                пагінацією OFFSET/FETCH та виключенням м'яко видалених записів
                (лише IsDeleted = 0).
   ------------------------------------------------------------------------- */
CREATE OR ALTER PROCEDURE dbo.usp_GetClientAppointments
    @ClientId   BIGINT,
    @Status     NVARCHAR(30) = NULL,
    @PageNumber INT          = 1,
    @PageSize   INT          = 10
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @PageNumber < 1 SET @PageNumber = 1;
    IF @PageSize < 1 SET @PageSize = 10;

    SELECT
        a.Id            AS AppointmentId,
        a.ClientId,
        c.FullName      AS ClientName,
        a.Status,
        a.ScheduledAt,
        a.TotalAmount,
        d.CarBrand,
        d.CarModel,
        d.PlateNumber,
        ISNULL(p.PaidAmount, 0.00) AS PaidAmount,
        a.CreatedAt,
        a.UpdatedAt
    FROM dbo.Appointments a
    INNER JOIN dbo.Clients c ON c.Id = a.ClientId
    LEFT JOIN dbo.AppointmentDetails d ON d.AppointmentId = a.Id
    OUTER APPLY (
        SELECT SUM(pay.Amount) AS PaidAmount
        FROM dbo.Payments pay
        WHERE pay.AppointmentId = a.Id
    ) p
    WHERE a.ClientId = @ClientId
      AND a.IsDeleted = 0
      AND (@Status IS NULL OR a.Status = @Status)
    ORDER BY a.ScheduledAt DESC, a.Id DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO
