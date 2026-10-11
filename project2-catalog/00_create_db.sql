/* =========================================================================
   Проєкт №2: Контекст "Каталог послуг" (Catalog)
   Файл: 00_create_db.sql
   Опис: Ідемпотентне створення бази даних DetailingCatalog з нуля.
   ========================================================================= */

USE [master];
GO

IF DB_ID(N'DetailingCatalog') IS NULL
BEGIN
    PRINT N'Створення бази даних [DetailingCatalog]...';
    CREATE DATABASE [DetailingCatalog];
    PRINT N'Базу даних [DetailingCatalog] успішно створено.';
END
ELSE
BEGIN
    PRINT N'База даних [DetailingCatalog] вже існує. Пропускаємо створення.';
END
GO
