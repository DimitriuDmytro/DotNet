IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [dbo].[Packages] (
    [Id] bigint NOT NULL IDENTITY,
    [Name] nvarchar(150) NOT NULL,
    [Description] nvarchar(1000) NULL,
    [Price] decimal(18,2) NOT NULL,
    [CreatedAt] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
    [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'system',
    [UpdatedAt] datetime2(3) NULL,
    [UpdatedBy] nvarchar(100) NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Packages] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Packages_Price] CHECK ([Price] > 0)
);

CREATE TABLE [dbo].[ServiceCategories] (
    [Id] bigint NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NULL,
    CONSTRAINT [PK_ServiceCategories] PRIMARY KEY ([Id])
);

CREATE TABLE [dbo].[Services] (
    [Id] bigint NOT NULL IDENTITY,
    [CategoryId] bigint NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    [DurationMin] int NOT NULL,
    [CreatedAt] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
    [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'system',
    [UpdatedAt] datetime2(3) NULL,
    [UpdatedBy] nvarchar(100) NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Services] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Services_DurationMin] CHECK ([DurationMin] > 0),
    CONSTRAINT [CK_Services_Price] CHECK ([Price] > 0),
    CONSTRAINT [FK_Services_ServiceCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [dbo].[ServiceCategories] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [dbo].[PackageServices] (
    [PackageId] bigint NOT NULL,
    [ServiceId] bigint NOT NULL,
    [SortOrder] int NOT NULL DEFAULT 1,
    [AddedAt] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_PackageServices] PRIMARY KEY ([PackageId], [ServiceId]),
    CONSTRAINT [CK_PackageServices_SortOrder] CHECK ([SortOrder] > 0),
    CONSTRAINT [FK_PackageServices_Packages_PackageId] FOREIGN KEY ([PackageId]) REFERENCES [dbo].[Packages] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_PackageServices_Services_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES [dbo].[Services] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [dbo].[ServiceImages] (
    [Id] bigint NOT NULL IDENTITY,
    [ServiceId] bigint NOT NULL,
    [Url] nvarchar(500) NOT NULL,
    [IsMain] bit NOT NULL DEFAULT CAST(0 AS bit),
    CONSTRAINT [PK_ServiceImages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ServiceImages_Services_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES [dbo].[Services] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [dbo].[ServiceSpecs] (
    [ServiceId] bigint NOT NULL,
    [Materials] nvarchar(500) NOT NULL,
    [WarrantyMonths] int NOT NULL DEFAULT 0,
    [Description] nvarchar(2000) NOT NULL,
    CONSTRAINT [PK_ServiceSpecs] PRIMARY KEY ([ServiceId]),
    CONSTRAINT [CK_ServiceSpecs_WarrantyMonths] CHECK ([WarrantyMonths] >= 0),
    CONSTRAINT [FK_ServiceSpecs_Services_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES [dbo].[Services] ([Id]) ON DELETE CASCADE
);

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'Name') AND [object_id] = OBJECT_ID(N'[dbo].[ServiceCategories]'))
    SET IDENTITY_INSERT [dbo].[ServiceCategories] ON;
INSERT INTO [dbo].[ServiceCategories] ([Id], [Description], [Name])
VALUES (CAST(1 AS bigint), N'Дбайлива багатофазна детейлінг-мийка кузова, підвіски та дисків.', N'Мийка'),
(CAST(2 AS bigint), N'Відновлювальне, коригувальне та глибоке абразивне полірування кузова і оптики.', N'Полірування'),
(CAST(3 AS bigint), N'Нанесення кераміки 9H, рідкого скла, антидощу та захисних плівок PPF.', N'Захист'),
(CAST(4 AS bigint), N'Комплексна хімчистка інтер''єру, озонування та консервація шкіри.', N'Салон');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'Name') AND [object_id] = OBJECT_ID(N'[dbo].[ServiceCategories]'))
    SET IDENTITY_INSERT [dbo].[ServiceCategories] OFF;

CREATE UNIQUE INDEX [UQ_Packages_Name] ON [dbo].[Packages] ([Name]);

CREATE INDEX [IX_PackageServices_ServiceId] ON [dbo].[PackageServices] ([ServiceId]);

CREATE UNIQUE INDEX [UQ_ServiceCategories_Name] ON [dbo].[ServiceCategories] ([Name]);

CREATE INDEX [IX_ServiceImages_ServiceId_IsMain] ON [dbo].[ServiceImages] ([ServiceId], [IsMain]);

CREATE UNIQUE INDEX [UX_ServiceImages_ServiceId_MainOnly] ON [dbo].[ServiceImages] ([ServiceId]) WHERE [IsMain] = 1;

CREATE INDEX [IX_Services_CategoryId_Price] ON [dbo].[Services] ([CategoryId], [Price]);

CREATE UNIQUE INDEX [IX_Services_Name] ON [dbo].[Services] ([Name]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261025203233_InitialCreate', N'9.0.2');

COMMIT;
GO

