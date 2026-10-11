# ER-діаграма Проєкту №1: `DetailingBookings` (SQL Server)

Нижче наведено ER-діаграму бази даних `DetailingBookings` (контекст **Bookings**).

> [!IMPORTANT]
> Таблиця `Services` у базі `DetailingBookings` є локальною read-model копією довідника з бази `DetailingCatalog` (`Id` **без `IDENTITY`**). Зв'язок із зовнішнім контекстом `Catalog` показаний пунктирною лінією (`..`), оскільки міжбазовий `FOREIGN KEY` між мікросервісами суворо заборонений.

```mermaid
erDiagram
    Clients ||--o{ Appointments : "1:N (ON DELETE NO ACTION)"
    Appointments ||--|| AppointmentDetails : "1:1 (PK=FK, ON DELETE CASCADE)"
    Appointments ||--|{ AppointmentItems : "1:N частина M:N (ON DELETE CASCADE)"
    Services ||--o{ AppointmentItems : "1:N частина M:N (локальна репліка)"
    Appointments ||--o{ Payments : "1:N (ON DELETE NO ACTION)"
    Appointments ||--o{ AppointmentStatusHistory : "1:N (ON DELETE NO ACTION)"
    EXTERNAL_Catalog_Service ||..|| Services : "Логічне посилання за ServiceId (НЕ FK, синхронізація подіями)"

    Clients {
        bigint Id PK "IDENTITY(1,1)"
        nvarchar_255 Email UK "NOT NULL UNIQUE"
        nvarchar_200 FullName "NOT NULL"
        nvarchar_30 Phone "NOT NULL"
        datetime2 CreatedAt "DEFAULT SYSUTCDATETIME()"
        nvarchar_100 CreatedBy "DEFAULT 'system'"
        datetime2 UpdatedAt "NULL"
        nvarchar_100 UpdatedBy "NULL"
        bit IsDeleted "DEFAULT 0 (Soft Delete)"
    }

    Appointments {
        bigint Id PK "IDENTITY(1,1)"
        bigint ClientId FK "REFERENCES Clients(Id)"
        nvarchar_30 Status "CHECK (Booked..Delivered, Cancelled)"
        datetime2 ScheduledAt "NOT NULL"
        decimal_18_2 TotalAmount "CHECK >= 0"
        datetime2 CreatedAt "DEFAULT SYSUTCDATETIME()"
        nvarchar_100 CreatedBy "DEFAULT 'system'"
        datetime2 UpdatedAt "NULL"
        nvarchar_100 UpdatedBy "NULL"
        bit IsDeleted "DEFAULT 0"
        rowversion RowVersion "Optimistic Concurrency"
    }

    AppointmentDetails {
        bigint AppointmentId PK,FK "1:1 REFERENCES Appointments(Id) CASCADE"
        nvarchar_100 CarBrand "NOT NULL"
        nvarchar_100 CarModel "NOT NULL"
        nvarchar_20 PlateNumber "NOT NULL"
        nvarchar_1000 Comment "NULL"
    }

    Services {
        bigint Id PK "БЕЗ IDENTITY (той самий Id, що в DetailingCatalog)"
        nvarchar_200 Name "Копія для читання"
        decimal_18_2 Price "CHECK > 0"
        int DurationMin "CHECK > 0"
        datetime2 SyncedAt "DEFAULT SYSUTCDATETIME()"
    }

    AppointmentItems {
        bigint AppointmentId PK,FK "Composite PK, CASCADE"
        bigint ServiceId PK,FK "Composite PK (логічне посилання на Каталог)"
        nvarchar_200 ServiceName "Snapshot на момент запису"
        decimal_18_2 UnitPrice "Snapshot на момент запису, CHECK > 0"
        decimal_18_2 Discount "CHECK >= 0"
        int Quantity "CHECK > 0"
    }

    Payments {
        bigint Id PK "IDENTITY(1,1)"
        bigint AppointmentId FK "REFERENCES Appointments(Id)"
        decimal_18_2 Amount "CHECK > 0"
        nvarchar_30 Method "CHECK (Cash, Card, BankTransfer)"
        datetime2 PaidAt "DEFAULT SYSUTCDATETIME()"
    }

    AppointmentStatusHistory {
        bigint Id PK "IDENTITY(1,1)"
        bigint AppointmentId FK "REFERENCES Appointments(Id)"
        nvarchar_30 OldStatus "NULL"
        nvarchar_30 NewStatus "NOT NULL"
        datetime2 ChangedAt "DEFAULT SYSUTCDATETIME()"
        nvarchar_100 ChangedBy "DEFAULT 'system'"
    }

    EXTERNAL_Catalog_Service {
        bigint Id PK "Зовнішня БД DetailingCatalog (БЕЗ FK)"
    }
```

## Типи зв'язків у схемі `DetailingBookings`

| Батьківська сутність | Дочірня сутність | Кардинальність | Ключі | Поведінка при видаленні |
| :--- | :--- | :--- | :--- | :--- |
| `Clients` | `Appointments` | `1:N` | `Appointments.ClientId -> Clients.Id` | `ON DELETE NO ACTION` (використовується `IsDeleted`) |
| `Appointments` | `AppointmentDetails` | `1:1` | `AppointmentDetails.AppointmentId (PK = FK) -> Appointments.Id` | `ON DELETE CASCADE` |
| `Appointments` $\leftrightarrow$ `Services` | `AppointmentItems` | `M:N` | Складений PK `(AppointmentId, ServiceId)` | `ON DELETE CASCADE` від `Appointments` |
| `Appointments` | `Payments` | `1:N` | `Payments.AppointmentId -> Appointments.Id` | `ON DELETE NO ACTION` |
| `Appointments` | `AppointmentStatusHistory` | `1:N` | `AppointmentStatusHistory.AppointmentId -> Appointments.Id` | `ON DELETE NO ACTION` |
| `DetailingCatalog.Services` | `DetailingBookings.Services` | Логічна репліка | Спільне значення `Id` (без `IDENTITY` у Bookings) | Міжбазовий FK **відсутній** (пунктир `..`) |
