-- =====================================================================
-- Скрипт створення таблиці ProcessedEvents для забезпечення ідемпотентності
-- споживача подій брокера RabbitMQ у мікросервісі Bookings (Лаб. 08, Критерій 9).
-- =====================================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProcessedEvents' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.ProcessedEvents (
        EventId UNIQUEIDENTIFIER NOT NULL,
        ProcessedAt DATETIME2 NOT NULL CONSTRAINT DF_ProcessedEvents_ProcessedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_ProcessedEvents PRIMARY KEY CLUSTERED (EventId)
    );
END
GO
