/* =========================================================================
   Проєкт №1: Контекст "Записи" (Bookings)
   Файл: 00_create_db.sql
   Опис: Ідемпотентне створення бази даних DetailingBookings з нуля.
   ========================================================================= */

USE [master];
GO

IF DB_ID(N'DetailingBookings') IS NULL
BEGIN
    PRINT N'Створення бази даних [DetailingBookings]...';
    CREATE DATABASE [DetailingBookings];
    PRINT N'Базу даних [DetailingBookings] успішно створено.';
END
ELSE
BEGIN
    PRINT N'База даних [DetailingBookings] вже існує. Пропускаємо створення.';
END
GO
