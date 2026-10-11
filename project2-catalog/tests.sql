/* =========================================================================
   Проєкт №2: Контекст "Каталог послуг" (Catalog)
   Файл: tests.sql
   База даних: DetailingCatalog
   Опис: Набір перевірочних запитів та тестів цілісності для бази Каталогу:
         1. Повна вибірка послуг із категорією (1:N), специфікацією (1:1)
            та головним зображенням (1:N).
         2. Вибірка пакетів (Packages) із деталізацією послуг (M:N) та
            розрахунком фінансової вигоди для клієнта.
         3. Перевірка запитів під створені індекси (за категорією/ціною та за назвою).
         4. Перевірка обмежень CHECK та UNIQUE у блоках TRY/CATCH.
         5. Аудит зовнішніх ключів (SELECT * FROM sys.foreign_keys).
   ========================================================================= */

USE [DetailingCatalog];
GO

SET NOCOUNT ON;
GO

PRINT N'=========================================================================';
PRINT N'ЧАСТИНА 1. КАТАЛОГ ПОСЛУГ ЗІ СПЕЦИФІКАЦІЯМИ (1:1) ТА ГОЛОВНИМ ФОТО (1:N)';
PRINT N'=========================================================================';

SELECT
    s.Id                AS ServiceId,
    c.Name              AS CategoryName,
    s.Name              AS ServiceName,
    s.Price,
    s.DurationMin,
    sp.Materials,
    sp.WarrantyMonths,
    img.Url             AS MainImageUrl
FROM dbo.Services s
INNER JOIN dbo.ServiceCategories c ON c.Id = s.CategoryId
LEFT JOIN dbo.ServiceSpecs sp ON sp.ServiceId = s.Id
LEFT JOIN dbo.ServiceImages img ON img.ServiceId = s.Id AND img.IsMain = 1
WHERE s.IsDeleted = 0
ORDER BY c.Id ASC, s.Price ASC;
GO

PRINT N'';
PRINT N'=========================================================================';
PRINT N'ЧАСТИНА 2. СКЛАД ПАКЕТІВ (M:N) ТА ОБЧИСЛЕННЯ ЕКОНОМІЇ КЛІЄНТА';
PRINT N'=========================================================================';

-- 2.1. Зведений аналіз пакетів: пакетна ціна проти суми окремих послуг
SELECT
    p.Id                                    AS PackageId,
    p.Name                                  AS PackageName,
    COUNT(ps.ServiceId)                     AS IncludedServicesCount,
    SUM(s.DurationMin)                      AS TotalDurationMin,
    SUM(s.Price)                            AS SeparateServicesSum,
    p.Price                                 AS PackagePrice,
    (SUM(s.Price) - p.Price)                AS ClientSavingsAmount
FROM dbo.Packages p
INNER JOIN dbo.PackageServices ps ON ps.PackageId = p.Id
INNER JOIN dbo.Services s ON s.Id = ps.ServiceId
WHERE p.IsDeleted = 0
GROUP BY p.Id, p.Name, p.Price
ORDER BY p.Price ASC;

-- 2.2. Покроковий порядок виконання послуг у кожному пакеті (за SortOrder)
SELECT
    p.Name          AS PackageName,
    ps.SortOrder,
    s.Id            AS ServiceId,
    s.Name          AS ServiceName,
    s.Price         AS RegularServicePrice,
    s.DurationMin
FROM dbo.Packages p
INNER JOIN dbo.PackageServices ps ON ps.PackageId = p.Id
INNER JOIN dbo.Services s ON s.Id = ps.ServiceId
WHERE p.IsDeleted = 0
ORDER BY p.Id ASC, ps.SortOrder ASC;
GO

PRINT N'';
PRINT N'=========================================================================';
PRINT N'ЧАСТИНА 3. ЗАПИТИ ПІД ІНДЕКСИ IX_Services_CategoryId_Price ТА IX_Services_Name';
PRINT N'=========================================================================';

-- 3.1. Використання індексу IX_Services_CategoryId_Price:
-- Послуги категорії "Захист" (CategoryId = 3) з ціною від 1500 до 15000 грн
SELECT
    Id,
    CategoryId,
    Name,
    Price,
    DurationMin
FROM dbo.Services
WHERE CategoryId = 3
  AND Price BETWEEN 1500.00 AND 15000.00
  AND IsDeleted = 0
ORDER BY Price ASC;

-- 3.2. Використання індексу IX_Services_Name:
-- Швидкий пошук за префіксом назви
SELECT
    Id,
    Name,
    Price,
    DurationMin
FROM dbo.Services
WHERE Name LIKE N'Полірування%'
  AND IsDeleted = 0;
GO

PRINT N'';
PRINT N'=========================================================================';
PRINT N'ЧАСТИНА 4. НЕГАТИВНІ ТЕСТИ ОБМЕЖЕНЬ ЦІЛІСНОСТІ (TRY/CATCH)';
PRINT N'=========================================================================';

-- 4.1. Перевірка обмеження CK_Services_Price (Price > 0)
BEGIN TRY
    PRINT N'[Негативний тест 4.1] Спроба додати послугу з від''ємною ціною (-500.00)...';
    INSERT INTO dbo.Services (CategoryId, Name, Price, DurationMin)
    VALUES (1, N'Некоректна послуга', -500.00, 30);
    PRINT N'ПОМИЛКА ТЕСТУ: Вставка з Price <= 0 не мала пройти!';
END TRY
BEGIN CATCH
    PRINT FORMATMESSAGE(
        N'[ОЧІКУВАНА ПОМИЛКА ОБМЕЖЕННЯ CHECK] Код: %d | Повідомлення: %s',
        ERROR_NUMBER(),
        ERROR_MESSAGE()
    );
END CATCH;

-- 4.2. Перевірка обмеження UQ_Services_Name (унікальна назва послуги)
BEGIN TRY
    PRINT N'[Негативний тест 4.2] Спроба додати дублікат назви послуги "Двофазна детейлінг-мийка"...';
    INSERT INTO dbo.Services (CategoryId, Name, Price, DurationMin)
    VALUES (1, N'Двофазна детейлінг-мийка', 950.00, 60);
    PRINT N'ПОМИЛКА ТЕСТУ: Дублікат назви не мав пройти!';
END TRY
BEGIN CATCH
    PRINT FORMATMESSAGE(
        N'[ОЧІКУВАНА ПОМИЛКА ОБМЕЖЕННЯ UNIQUE] Код: %d | Повідомлення: %s',
        ERROR_NUMBER(),
        ERROR_MESSAGE()
    );
END CATCH;
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
