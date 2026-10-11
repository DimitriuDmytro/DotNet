/* =========================================================================
   Проєкт №2: Контекст "Каталог послуг" (Catalog)
   Файл: seed.sql
   База даних: DetailingCatalog
   Опис: Ідемпотентне наповнення бази даних:
         - 4 категорії (Мийка, Полірування, Захист, Салон)
         - 12 послуг (Id 1..10 повністю збігаються з локальною реплікою в Проєкті №1)
         - 12 технічних специфікацій (ServiceSpecs 1:1)
         - Фотогалерея послуг (ServiceImages 1:N)
         - 3 пакети (Basic, Premium, Ceramic Pro) та їх зв'язки з послугами (PackageServices M:N)
   ========================================================================= */

USE [DetailingCatalog];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

PRINT N'=== Початок заповнення бази [DetailingCatalog] (seed.sql) ===';

/* -------------------------------------------------------------------------
   1. Категорії послуг ServiceCategories (4 категорії)
   ------------------------------------------------------------------------- */
SET IDENTITY_INSERT dbo.ServiceCategories ON;

IF NOT EXISTS (SELECT 1 FROM dbo.ServiceCategories WHERE Id = 1 OR Name = N'Мийка')
    INSERT INTO dbo.ServiceCategories (Id, Name, Description)
    VALUES (1, N'Мийка', N'Дбайлива багатофазна детейлінг-мийка кузова, підвіски, дисків та глибоке очищення ЛФП.');

IF NOT EXISTS (SELECT 1 FROM dbo.ServiceCategories WHERE Id = 2 OR Name = N'Полірування')
    INSERT INTO dbo.ServiceCategories (Id, Name, Description)
    VALUES (2, N'Полірування', N'Відновлювальне, коригувальне та глибоке абразивне полірування кузова і оптики.');

IF NOT EXISTS (SELECT 1 FROM dbo.ServiceCategories WHERE Id = 3 OR Name = N'Захист')
    INSERT INTO dbo.ServiceCategories (Id, Name, Description)
    VALUES (3, N'Захист', N'Нанесення кварцових, керамічних складів, рідкого скла, антидощу та захисних плівок PPF.');

IF NOT EXISTS (SELECT 1 FROM dbo.ServiceCategories WHERE Id = 4 OR Name = N'Салон')
    INSERT INTO dbo.ServiceCategories (Id, Name, Description)
    VALUES (4, N'Салон', N'Комплексна хімчистка інтер''єру, озонування, реставрація та консервація шкіри і пластику.');

SET IDENTITY_INSERT dbo.ServiceCategories OFF;
PRINT N'Категорії ServiceCategories (4 записи) перевірені/додані.';
GO

/* -------------------------------------------------------------------------
   2. Послуги Services (12 послуг, Id 1..10 збігаються з DetailingBookings)
   ------------------------------------------------------------------------- */
SET IDENTITY_INSERT dbo.Services ON;

IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE Id = 1 OR Name = N'Двофазна детейлінг-мийка')
    INSERT INTO dbo.Services (Id, CategoryId, Name, Price, DurationMin, CreatedBy)
    VALUES (1, 1, N'Двофазна детейлінг-мийка', 800.00, 60, N'catalog.editor');

IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE Id = 2 OR Name = N'Трифазна нано-мийка з консервацією')
    INSERT INTO dbo.Services (Id, CategoryId, Name, Price, DurationMin, CreatedBy)
    VALUES (2, 1, N'Трифазна нано-мийка з консервацією', 1200.00, 90, N'catalog.editor');

IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE Id = 3 OR Name = N'Деконтамінація кузова та дисків')
    INSERT INTO dbo.Services (Id, CategoryId, Name, Price, DurationMin, CreatedBy)
    VALUES (3, 1, N'Деконтамінація кузова та дисків', 1500.00, 120, N'catalog.editor');

IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE Id = 4 OR Name = N'Легке відновлювальне полірування')
    INSERT INTO dbo.Services (Id, CategoryId, Name, Price, DurationMin, CreatedBy)
    VALUES (4, 2, N'Легке відновлювальне полірування', 4500.00, 360, N'catalog.editor');

IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE Id = 5 OR Name = N'Глибоке абразивне полірування кузова')
    INSERT INTO dbo.Services (Id, CategoryId, Name, Price, DurationMin, CreatedBy)
    VALUES (5, 2, N'Глибоке абразивне полірування кузова', 8500.00, 600, N'catalog.editor');

IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE Id = 6 OR Name = N'Полірування фар та ліхтарів')
    INSERT INTO dbo.Services (Id, CategoryId, Name, Price, DurationMin, CreatedBy)
    VALUES (6, 2, N'Полірування фар та ліхтарів', 1400.00, 90, N'catalog.editor');

IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE Id = 7 OR Name = N'Керамічне покриття кузова 9H')
    INSERT INTO dbo.Services (Id, CategoryId, Name, Price, DurationMin, CreatedBy)
    VALUES (7, 3, N'Керамічне покриття кузова 9H', 12000.00, 720, N'catalog.editor');

IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE Id = 8 OR Name = N'Антидощ на всі вікна')
    INSERT INTO dbo.Services (Id, CategoryId, Name, Price, DurationMin, CreatedBy)
    VALUES (8, 3, N'Антидощ на всі вікна', 1800.00, 120, N'catalog.editor');

IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE Id = 9 OR Name = N'Повна хімчистка салону з розбором')
    INSERT INTO dbo.Services (Id, CategoryId, Name, Price, DurationMin, CreatedBy)
    VALUES (9, 4, N'Повна хімчистка салону з розбором', 5500.00, 480, N'catalog.editor');

IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE Id = 10 OR Name = N'Озонування та антибактеріальна обробка')
    INSERT INTO dbo.Services (Id, CategoryId, Name, Price, DurationMin, CreatedBy)
    VALUES (10, 4, N'Озонування та антибактеріальна обробка', 900.00, 45, N'catalog.editor');

IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE Id = 11 OR Name = N'Захист шкіри салону керамічним складом')
    INSERT INTO dbo.Services (Id, CategoryId, Name, Price, DurationMin, CreatedBy)
    VALUES (11, 4, N'Захист шкіри салону керамічним складом', 2800.00, 180, N'catalog.editor');

IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE Id = 12 OR Name = N'Оклейка зон ризику антигравійною плівкою PPF')
    INSERT INTO dbo.Services (Id, CategoryId, Name, Price, DurationMin, CreatedBy)
    VALUES (12, 3, N'Оклейка зон ризику антигравійною плівкою PPF', 16500.00, 540, N'catalog.editor');

SET IDENTITY_INSERT dbo.Services OFF;
PRINT N'Послуги Services (12 записів) перевірені/додані.';
GO

/* -------------------------------------------------------------------------
   3. Технічні специфікації ServiceSpecs (1:1 до Services)
   ------------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM dbo.ServiceSpecs WHERE ServiceId = 1)
    INSERT INTO dbo.ServiceSpecs (ServiceId, Materials, WarrantyMonths, Description)
    VALUES (1, N'Koch Chemie Green Star, Gentle Snow Foam, мікрофіброві рукавиці', 0, N'Безконтактна передмийка активною піною та ручна мийка шампунем за системою двох відер із сушінням турбосушкою.');

IF NOT EXISTS (SELECT 1 FROM dbo.ServiceSpecs WHERE ServiceId = 2)
    INSERT INTO dbo.ServiceSpecs (ServiceId, Materials, WarrantyMonths, Description)
    VALUES (2, N'Koch Chemie NanoMagicShampoo, ProtectorWax', 1, N'Трифазне очищення кузова з нанесенням гідрофобного консервуючого воску преміум-класу.');

IF NOT EXISTS (SELECT 1 FROM dbo.ServiceSpecs WHERE ServiceId = 3)
    INSERT INTO dbo.ServiceSpecs (ServiceId, Materials, WarrantyMonths, Description)
    VALUES (3, N'Gyeon Q2M Iron, Q2M Tar, синтетична глина Clay Bar', 0, N'Видалення металевих вкраплень, колодкового пилу, бітуму та деревних смол з поверхні ЛФП і колісних дисків.');

IF NOT EXISTS (SELECT 1 FROM dbo.ServiceSpecs WHERE ServiceId = 4)
    INSERT INTO dbo.ServiceSpecs (ServiceId, Materials, WarrantyMonths, Description)
    VALUES (4, N'Menzerna Medium Cut 2500, Super Finish 3800, круги Rupes', 6, N'Одно-двокрокове м''яке полірування для усунення дрібної павутинки (до 70% подряпин) і повернення глибокого глянцю.');

IF NOT EXISTS (SELECT 1 FROM dbo.ServiceSpecs WHERE ServiceId = 5)
    INSERT INTO dbo.ServiceSpecs (ServiceId, Materials, WarrantyMonths, Description)
    VALUES (5, N'Koch Chemie H9.02 Heavy Cut, F6.01, M3.02, хутряні та поролонові круги Rupes', 12, N'Трьохетапна глибока корекція лакофарбового покриття з усуненням до 95% подряпин, голограм та оксидного шару.');

IF NOT EXISTS (SELECT 1 FROM dbo.ServiceSpecs WHERE ServiceId = 6)
    INSERT INTO dbo.ServiceSpecs (ServiceId, Materials, WarrantyMonths, Description)
    VALUES (6, N'Абразиви Mirka Abralon, паста Menzerna 400 / 3800', 6, N'Відновлення прозорості полікарбонатних лінз передніх фар та задніх ліхтарів.');

IF NOT EXISTS (SELECT 1 FROM dbo.ServiceSpecs WHERE ServiceId = 7)
    INSERT INTO dbo.ServiceSpecs (ServiceId, Materials, WarrantyMonths, Description)
    VALUES (7, N'Gyeon Q2 Mohs EVO (2 шари) + Q2 Cure', 24, N'Двошарове кварцово-керамічне покриття твердістю 9H з потужним гідрофобним ефектом, захистом від УФ та реагентів.');

IF NOT EXISTS (SELECT 1 FROM dbo.ServiceSpecs WHERE ServiceId = 8)
    INSERT INTO dbo.ServiceSpecs (ServiceId, Materials, WarrantyMonths, Description)
    VALUES (8, N'Aquapel Glass Treatment / Gyeon Q2 View', 6, N'Полірування скла та нанесення фторполімерного гідрофобного покриття на лобове, бічні та заднє скло.');

IF NOT EXISTS (SELECT 1 FROM dbo.ServiceSpecs WHERE ServiceId = 9)
    INSERT INTO dbo.ServiceSpecs (ServiceId, Materials, WarrantyMonths, Description)
    VALUES (9, N'Koch Chemie Mehrzweckreiniger, Pol Star, Leather Star, торнадор і екстрактор', 3, N'Глибока хімчистка стелі, підлоги, сидінь, ременів безпеки та багажника з демонтажем крісел і делікатною сушкою.');

IF NOT EXISTS (SELECT 1 FROM dbo.ServiceSpecs WHERE ServiceId = 10)
    INSERT INTO dbo.ServiceSpecs (ServiceId, Materials, WarrantyMonths, Description)
    VALUES (10, N'Промисловий генератор озону O3 + нейтралізатор запахів Koch Chemie Fresh Up', 1, N'Дезінфекція системи кондиціонування та салону озоном, повне знищення бактерій, грибка та запаху тютюну.');

IF NOT EXISTS (SELECT 1 FROM dbo.ServiceSpecs WHERE ServiceId = 11)
    INSERT INTO dbo.ServiceSpecs (ServiceId, Materials, WarrantyMonths, Description)
    VALUES (11, N'Gyeon Q2 LeatherShield / Colourlock Leather Shield', 12, N'Захисне нанопокриття для шкіряних сидінь та керма проти потертостей, фарбування від джинсів та пересихання.');

IF NOT EXISTS (SELECT 1 FROM dbo.ServiceSpecs WHERE ServiceId = 12)
    INSERT INTO dbo.ServiceSpecs (ServiceId, Materials, WarrantyMonths, Description)
    VALUES (12, N'Поліуретанова плівка LLumar Platinum PPF (200 мкм)', 60, N'Бронювання переднього бампера, капота, крил, фар, дзеркал і зон під ручками антигравійною плівкою з ефектом самовідновлення.');

PRINT N'Технічні специфікації ServiceSpecs (12 записів) перевірені/додані.';
GO

/* -------------------------------------------------------------------------
   4. Пакетні пропозиції Packages (3 пакети: Basic, Premium, Ceramic Pro)
   ------------------------------------------------------------------------- */
SET IDENTITY_INSERT dbo.Packages ON;

-- Basic: мийка (800) + деконтамінація (1500) + антидощ (1800) + озонування (900) = 5000 -> ціна пакета 4200.00
IF NOT EXISTS (SELECT 1 FROM dbo.Packages WHERE Id = 1 OR Name = N'Basic')
    INSERT INTO dbo.Packages (Id, Name, Description, Price, CreatedBy)
    VALUES (1, N'Basic', N'Базовий сезонний догляд: двофазна мийка, глибока деконтамінація кузова, антидощ на всі вікна та озонування салону.', 4200.00, N'catalog.editor');

-- Premium: трифазна мийка (1200) + легке полірування (4500) + полірування фар (1400) + хімчистка салону (5500) = 12600 -> ціна пакета 10800.00
IF NOT EXISTS (SELECT 1 FROM dbo.Packages WHERE Id = 2 OR Name = N'Premium')
    INSERT INTO dbo.Packages (Id, Name, Description, Price, CreatedBy)
    VALUES (2, N'Premium', N'Комплексне оновлення екстер''єру та інтер''єру: трифазна мийка, відновлювальне полірування кузова і фар, повна хімчистка салону.', 10800.00, N'catalog.editor');

-- Ceramic Pro: деконтамінація (1500) + глибоке полірування (8500) + кераміка 9H (12000) + антидощ (1800) + захист шкіри (2800) = 26600 -> ціна пакета 22900.00
IF NOT EXISTS (SELECT 1 FROM dbo.Packages WHERE Id = 3 OR Name = N'Ceramic Pro')
    INSERT INTO dbo.Packages (Id, Name, Description, Price, CreatedBy)
    VALUES (3, N'Ceramic Pro', N'Максимальний преміум-комплекс: деконтамінація, глибоке абразивне полірування, кераміка кузова 9H, антидощ і керамічний захист шкіри.', 22900.00, N'catalog.editor');

SET IDENTITY_INSERT dbo.Packages OFF;
PRINT N'Пакети Packages (3 записи) перевірені/додані.';
GO

/* -------------------------------------------------------------------------
   5. Склад пакетів PackageServices (M:N із порядком виконання SortOrder)
   ------------------------------------------------------------------------- */
-- Пакет #1 (Basic)
IF NOT EXISTS (SELECT 1 FROM dbo.PackageServices WHERE PackageId = 1 AND ServiceId = 1)
    INSERT INTO dbo.PackageServices (PackageId, ServiceId, SortOrder) VALUES (1, 1, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.PackageServices WHERE PackageId = 1 AND ServiceId = 3)
    INSERT INTO dbo.PackageServices (PackageId, ServiceId, SortOrder) VALUES (1, 3, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.PackageServices WHERE PackageId = 1 AND ServiceId = 8)
    INSERT INTO dbo.PackageServices (PackageId, ServiceId, SortOrder) VALUES (1, 8, 3);
IF NOT EXISTS (SELECT 1 FROM dbo.PackageServices WHERE PackageId = 1 AND ServiceId = 10)
    INSERT INTO dbo.PackageServices (PackageId, ServiceId, SortOrder) VALUES (1, 10, 4);

-- Пакет #2 (Premium)
IF NOT EXISTS (SELECT 1 FROM dbo.PackageServices WHERE PackageId = 2 AND ServiceId = 2)
    INSERT INTO dbo.PackageServices (PackageId, ServiceId, SortOrder) VALUES (2, 2, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.PackageServices WHERE PackageId = 2 AND ServiceId = 9)
    INSERT INTO dbo.PackageServices (PackageId, ServiceId, SortOrder) VALUES (2, 9, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.PackageServices WHERE PackageId = 2 AND ServiceId = 4)
    INSERT INTO dbo.PackageServices (PackageId, ServiceId, SortOrder) VALUES (2, 4, 3);
IF NOT EXISTS (SELECT 1 FROM dbo.PackageServices WHERE PackageId = 2 AND ServiceId = 6)
    INSERT INTO dbo.PackageServices (PackageId, ServiceId, SortOrder) VALUES (2, 6, 4);

-- Пакет #3 (Ceramic Pro)
IF NOT EXISTS (SELECT 1 FROM dbo.PackageServices WHERE PackageId = 3 AND ServiceId = 3)
    INSERT INTO dbo.PackageServices (PackageId, ServiceId, SortOrder) VALUES (3, 3, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.PackageServices WHERE PackageId = 3 AND ServiceId = 5)
    INSERT INTO dbo.PackageServices (PackageId, ServiceId, SortOrder) VALUES (3, 5, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.PackageServices WHERE PackageId = 3 AND ServiceId = 7)
    INSERT INTO dbo.PackageServices (PackageId, ServiceId, SortOrder) VALUES (3, 7, 3);
IF NOT EXISTS (SELECT 1 FROM dbo.PackageServices WHERE PackageId = 3 AND ServiceId = 8)
    INSERT INTO dbo.PackageServices (PackageId, ServiceId, SortOrder) VALUES (3, 8, 4);
IF NOT EXISTS (SELECT 1 FROM dbo.PackageServices WHERE PackageId = 3 AND ServiceId = 11)
    INSERT INTO dbo.PackageServices (PackageId, ServiceId, SortOrder) VALUES (3, 11, 5);

PRINT N'Зв''язки PackageServices перевірені/додані.';
GO

/* -------------------------------------------------------------------------
   6. Фотогалерея послуг ServiceImages (1:N до Services)
   ------------------------------------------------------------------------- */
SET IDENTITY_INSERT dbo.ServiceImages ON;

IF NOT EXISTS (SELECT 1 FROM dbo.ServiceImages WHERE Id = 1)
    INSERT INTO dbo.ServiceImages (Id, ServiceId, Url, IsMain)
    VALUES (1, 1, N'https://cdn.detailing.ua/catalog/services/two-phase-wash-main.jpg', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.ServiceImages WHERE Id = 2)
    INSERT INTO dbo.ServiceImages (Id, ServiceId, Url, IsMain)
    VALUES (2, 5, N'https://cdn.detailing.ua/catalog/services/heavy-polish-main.jpg', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.ServiceImages WHERE Id = 3)
    INSERT INTO dbo.ServiceImages (Id, ServiceId, Url, IsMain)
    VALUES (3, 5, N'https://cdn.detailing.ua/catalog/services/heavy-polish-50-50.jpg', 0);

IF NOT EXISTS (SELECT 1 FROM dbo.ServiceImages WHERE Id = 4)
    INSERT INTO dbo.ServiceImages (Id, ServiceId, Url, IsMain)
    VALUES (4, 7, N'https://cdn.detailing.ua/catalog/services/ceramic-9h-main.jpg', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.ServiceImages WHERE Id = 5)
    INSERT INTO dbo.ServiceImages (Id, ServiceId, Url, IsMain)
    VALUES (5, 7, N'https://cdn.detailing.ua/catalog/services/ceramic-9h-hydrophobic.jpg', 0);

IF NOT EXISTS (SELECT 1 FROM dbo.ServiceImages WHERE Id = 6)
    INSERT INTO dbo.ServiceImages (Id, ServiceId, Url, IsMain)
    VALUES (6, 9, N'https://cdn.detailing.ua/catalog/services/interior-dryclean-main.jpg', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.ServiceImages WHERE Id = 7)
    INSERT INTO dbo.ServiceImages (Id, ServiceId, Url, IsMain)
    VALUES (7, 12, N'https://cdn.detailing.ua/catalog/services/ppf-armor-main.jpg', 1);

SET IDENTITY_INSERT dbo.ServiceImages OFF;
PRINT N'Зображення ServiceImages (7 записів) перевірені/додані.';
PRINT N'=== Заповнення бази [DetailingCatalog] успішно завершено ===';
GO
