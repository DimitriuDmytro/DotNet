/* =========================================================================
   Проєкт №2: Контекст "Каталог послуг" (Catalog)
   Файл: schema.sql
   База даних: DetailingCatalog
   Майбутній стек: Entity Framework Core (Code First)
   Опис: Ідемпотентне створення таблиць, обмежень та індексів.
         Таблиці названі у множині (ServiceCategories, Services, ServiceSpecs,
         Packages, PackageServices, ServiceImages) для повної сумісності з
         конвенціями DbSet<TEntity> у EF Core Code First.
   ========================================================================= */

USE [DetailingCatalog];
GO

SET NOCOUNT ON;
GO

/* -------------------------------------------------------------------------
   1. Таблиця ServiceCategories (сутність ServiceCategory) — категорії послуг
   Зв'язок 1:N з таблицею Services.
   ------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.ServiceCategories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ServiceCategories
    (
        Id          BIGINT          IDENTITY(1,1) NOT NULL,
        Name        NVARCHAR(100)   NOT NULL,
        Description NVARCHAR(500)   NULL,

        CONSTRAINT PK_ServiceCategories PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_ServiceCategories_Name UNIQUE (Name)
    );
    PRINT N'Таблицю [dbo.ServiceCategories] створено.';
END
GO

/* -------------------------------------------------------------------------
   2. Таблиця Services (сутність Service) — послуги детейлінг-студії (Корінь агрегата)
   Містить аудитні колонки, IsDeleted та RowVersion для оптимістичної конкурентності
   (у EF Core мапиться через .IsRowVersion()).
   ------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.Services', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Services
    (
        Id          BIGINT          IDENTITY(1,1) NOT NULL,
        CategoryId  BIGINT          NOT NULL,
        Name        NVARCHAR(200)   NOT NULL,
        Price       DECIMAL(18,2)   NOT NULL,
        DurationMin INT             NOT NULL,
        -- Службові колонки аудиту, м'якого видалення та оптимістичного блокування
        CreatedAt   DATETIME2(3)    NOT NULL CONSTRAINT DF_Services_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CreatedBy   NVARCHAR(100)   NOT NULL CONSTRAINT DF_Services_CreatedBy DEFAULT (N'system'),
        UpdatedAt   DATETIME2(3)    NULL,
        UpdatedBy   NVARCHAR(100)   NULL,
        IsDeleted   BIT             NOT NULL CONSTRAINT DF_Services_IsDeleted DEFAULT (0),
        RowVersion  ROWVERSION      NOT NULL,

        CONSTRAINT PK_Services PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Services_Name UNIQUE (Name),
        CONSTRAINT FK_Services_ServiceCategories FOREIGN KEY (CategoryId)
            REFERENCES dbo.ServiceCategories (Id) ON DELETE NO ACTION,
        CONSTRAINT CK_Services_Price CHECK (Price > 0),
        CONSTRAINT CK_Services_DurationMin CHECK (DurationMin > 0)
    );
    PRINT N'Таблицю [dbo.Services] створено.';
END
GO

/* -------------------------------------------------------------------------
   3. Таблиця ServiceSpecs (сутність ServiceSpec) — технічна специфікація послуги
   Зв'язок 1:1 із Services: PK одночасно є FK на Services(Id) з ON DELETE CASCADE.
   ------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.ServiceSpecs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ServiceSpecs
    (
        ServiceId      BIGINT          NOT NULL,
        Materials      NVARCHAR(500)   NOT NULL,
        WarrantyMonths INT             NOT NULL CONSTRAINT DF_ServiceSpecs_WarrantyMonths DEFAULT (0),
        Description    NVARCHAR(2000)  NOT NULL,

        CONSTRAINT PK_ServiceSpecs PRIMARY KEY CLUSTERED (ServiceId),
        CONSTRAINT FK_ServiceSpecs_Services FOREIGN KEY (ServiceId)
            REFERENCES dbo.Services (Id) ON DELETE CASCADE,
        CONSTRAINT CK_ServiceSpecs_WarrantyMonths CHECK (WarrantyMonths >= 0)
    );
    PRINT N'Таблицю [dbo.ServiceSpecs] створено.';
END
GO

/* -------------------------------------------------------------------------
   4. Таблиця Packages (сутність Package) — комплексні пакети послуг (Корінь агрегата)
   Містить аудитні колонки, IsDeleted та RowVersion.
   ------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.Packages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Packages
    (
        Id          BIGINT          IDENTITY(1,1) NOT NULL,
        Name        NVARCHAR(150)   NOT NULL,
        Description NVARCHAR(1000)  NULL,
        Price       DECIMAL(18,2)   NOT NULL,
        -- Службові колонки аудиту, м'якого видалення та оптимістичного блокування
        CreatedAt   DATETIME2(3)    NOT NULL CONSTRAINT DF_Packages_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CreatedBy   NVARCHAR(100)   NOT NULL CONSTRAINT DF_Packages_CreatedBy DEFAULT (N'system'),
        UpdatedAt   DATETIME2(3)    NULL,
        UpdatedBy   NVARCHAR(100)   NULL,
        IsDeleted   BIT             NOT NULL CONSTRAINT DF_Packages_IsDeleted DEFAULT (0),
        RowVersion  ROWVERSION      NOT NULL,

        CONSTRAINT PK_Packages PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Packages_Name UNIQUE (Name),
        CONSTRAINT CK_Packages_Price CHECK (Price > 0)
    );
    PRINT N'Таблицю [dbo.Packages] створено.';
END
GO

/* -------------------------------------------------------------------------
   5. Таблиця PackageServices (сутність PackageService) — зв'язок M:N між
   Packages та Services зі складеним PK (PackageId, ServiceId) і власними
   полями SortOrder та AddedAt.
   ------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.PackageServices', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PackageServices
    (
        PackageId BIGINT       NOT NULL,
        ServiceId BIGINT       NOT NULL,
        SortOrder INT          NOT NULL CONSTRAINT DF_PackageServices_SortOrder DEFAULT (1),
        AddedAt   DATETIME2(3) NOT NULL CONSTRAINT DF_PackageServices_AddedAt DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT PK_PackageServices PRIMARY KEY CLUSTERED (PackageId, ServiceId),
        CONSTRAINT FK_PackageServices_Packages FOREIGN KEY (PackageId)
            REFERENCES dbo.Packages (Id) ON DELETE CASCADE,
        CONSTRAINT FK_PackageServices_Services FOREIGN KEY (ServiceId)
            REFERENCES dbo.Services (Id) ON DELETE NO ACTION,
        CONSTRAINT CK_PackageServices_SortOrder CHECK (SortOrder > 0)
    );
    PRINT N'Таблицю [dbo.PackageServices] створено.';
END
GO

/* -------------------------------------------------------------------------
   6. Таблиця ServiceImages (сутність ServiceImage) — фотогалерея послуги (1:N)
   ------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.ServiceImages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ServiceImages
    (
        Id        BIGINT        IDENTITY(1,1) NOT NULL,
        ServiceId BIGINT        NOT NULL,
        Url       NVARCHAR(500) NOT NULL,
        IsMain    BIT           NOT NULL CONSTRAINT DF_ServiceImages_IsMain DEFAULT (0),

        CONSTRAINT PK_ServiceImages PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_ServiceImages_Services FOREIGN KEY (ServiceId)
            REFERENCES dbo.Services (Id) ON DELETE CASCADE
    );
    PRINT N'Таблицю [dbo.ServiceImages] створено.';
END
GO

/* =========================================================================
   ІНДЕКСИ (з коментарями, під який саме запит створено кожен індекс)
   ========================================================================= */

-- 1. Індекс Services(CategoryId, Price): під запит фільтрації послуг за категорією
--    із сортуванням за ціною або вибіркою в ціновому діапазоні (каталог на сайті)
-- Запит: SELECT ... FROM dbo.Services WHERE CategoryId = @CategoryId AND IsDeleted = 0 ORDER BY Price ASC;
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Services_CategoryId_Price'
      AND object_id = OBJECT_ID(N'dbo.Services')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Services_CategoryId_Price
        ON dbo.Services (CategoryId, Price)
        INCLUDE (Name, DurationMin, IsDeleted);
    PRINT N'Індекс [IX_Services_CategoryId_Price] створено.';
END
GO

-- 2. Індекс Services(Name): під запит швидкого текстового пошуку послуги за назвою
--    (автодоповнення у рядку пошуку клієнта та перевірка унікальності)
-- Запит: SELECT ... FROM dbo.Services WHERE Name LIKE @SearchPrefix + N'%' AND IsDeleted = 0;
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Services_Name'
      AND object_id = OBJECT_ID(N'dbo.Services')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Services_Name
        ON dbo.Services (Name)
        INCLUDE (CategoryId, Price, DurationMin, IsDeleted);
    PRINT N'Індекс [IX_Services_Name] створено.';
END
GO

-- 3. Допоміжний індекс ServiceImages(ServiceId, IsMain): під запит отримання головного фото послуги
-- Запит: SELECT Url FROM dbo.ServiceImages WHERE ServiceId = @ServiceId AND IsMain = 1;
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_ServiceImages_ServiceId_IsMain'
      AND object_id = OBJECT_ID(N'dbo.ServiceImages')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ServiceImages_ServiceId_IsMain
        ON dbo.ServiceImages (ServiceId, IsMain)
        INCLUDE (Url);
    PRINT N'Індекс [IX_ServiceImages_ServiceId_IsMain] створено.';
END
GO
