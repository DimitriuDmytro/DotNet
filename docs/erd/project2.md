# ER-діаграма Проєкту №2: `DetailingCatalog` (SQL Server)

Нижче наведено ER-діаграму бази даних `DetailingCatalog` (контекст **Catalog**), спроєктованої під подальше використання з **Entity Framework Core (Code First)**.

```mermaid
erDiagram
    ServiceCategories ||--o{ Services : "1:N (ON DELETE NO ACTION)"
    Services ||--|| ServiceSpecs : "1:1 (PK=FK, ON DELETE CASCADE)"
    Services ||--o{ ServiceImages : "1:N (ON DELETE CASCADE)"
    Packages ||--|{ PackageServices : "1:N частина M:N (ON DELETE CASCADE)"
    Services ||--o{ PackageServices : "1:N частина M:N (ON DELETE NO ACTION)"

    ServiceCategories {
        bigint Id PK "IDENTITY(1,1)"
        nvarchar_100 Name UK "NOT NULL UNIQUE"
        nvarchar_500 Description "NULL"
    }

    Services {
        bigint Id PK "IDENTITY(1,1) System of Record"
        bigint CategoryId FK "REFERENCES ServiceCategories(Id)"
        nvarchar_200 Name UK "NOT NULL UNIQUE"
        decimal_18_2 Price "CHECK > 0"
        int DurationMin "CHECK > 0"
        datetime2 CreatedAt "DEFAULT SYSUTCDATETIME()"
        nvarchar_100 CreatedBy "DEFAULT 'system'"
        datetime2 UpdatedAt "NULL"
        nvarchar_100 UpdatedBy "NULL"
        bit IsDeleted "DEFAULT 0 (Soft Delete)"
        rowversion RowVersion "Optimistic Concurrency"
    }

    ServiceSpecs {
        bigint ServiceId PK,FK "1:1 REFERENCES Services(Id) CASCADE"
        nvarchar_500 Materials "NOT NULL"
        int WarrantyMonths "CHECK >= 0"
        nvarchar_2000 Description "NOT NULL"
    }

    ServiceImages {
        bigint Id PK "IDENTITY(1,1)"
        bigint ServiceId FK "REFERENCES Services(Id) CASCADE"
        nvarchar_500 Url "NOT NULL"
        bit IsMain "DEFAULT 0"
    }

    Packages {
        bigint Id PK "IDENTITY(1,1)"
        nvarchar_150 Name UK "NOT NULL UNIQUE"
        nvarchar_1000 Description "NULL"
        decimal_18_2 Price "CHECK > 0"
        datetime2 CreatedAt "DEFAULT SYSUTCDATETIME()"
        nvarchar_100 CreatedBy "DEFAULT 'system'"
        datetime2 UpdatedAt "NULL"
        nvarchar_100 UpdatedBy "NULL"
        bit IsDeleted "DEFAULT 0 (Soft Delete)"
        rowversion RowVersion "Optimistic Concurrency"
    }

    PackageServices {
        bigint PackageId PK,FK "Composite PK, CASCADE"
        bigint ServiceId PK,FK "Composite PK, NO ACTION"
        int SortOrder "DEFAULT 1, CHECK > 0"
        datetime2 AddedAt "DEFAULT SYSUTCDATETIME()"
    }
```

## Типи зв'язків у схемі `DetailingCatalog`

| Батьківська сутність | Дочірня сутність | Кардинальність | Ключі | Поведінка при видаленні |
| :--- | :--- | :--- | :--- | :--- |
| `ServiceCategories` | `Services` | `1:N` | `Services.CategoryId -> ServiceCategories.Id` | `ON DELETE NO ACTION` |
| `Services` | `ServiceSpecs` | `1:1` | `ServiceSpecs.ServiceId (PK = FK) -> Services.Id` | `ON DELETE CASCADE` |
| `Services` | `ServiceImages` | `1:N` | `ServiceImages.ServiceId -> Services.Id` | `ON DELETE CASCADE` |
| `Packages` $\leftrightarrow$ `Services` | `PackageServices` | `M:N` | Складений PK `(PackageId, ServiceId)` + атрибути `SortOrder`, `AddedAt` | `CASCADE` від `Packages`, `NO ACTION` від `Services` |
