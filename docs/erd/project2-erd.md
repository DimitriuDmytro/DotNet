# ERD Діаграма сутностей: Каталог послуг (Detailing Catalog)

Діаграма реляційних зв'язків мікросервісу `Catalog` (Лабораторна робота №03).

```mermaid
erDiagram
    SERVICE_CATEGORIES ||--o{ SERVICES : "1:N містить"
    SERVICES ||--o| SERVICE_SPECS : "1:1 специфікація"
    SERVICES ||--o{ SERVICE_IMAGES : "1:N галерея"
    PACKAGES ||--o{ PACKAGE_SERVICES : "1:N"
    SERVICES ||--o{ PACKAGE_SERVICES : "1:N"

    SERVICE_CATEGORIES {
        bigint Id PK "IDENTITY(1,1)"
        nvarchar(100) Name "UNIQUE"
        nvarchar(500) Description "NULL"
    }

    SERVICES {
        bigint Id PK "IDENTITY(1,1)"
        bigint CategoryId FK
        nvarchar(200) Name "UNIQUE"
        decimal(10_2) Price "CK > 0"
        int DurationMin "CK > 0"
        datetime2 CreatedAt "DEFAULT SYSUTCDATETIME()"
        nvarchar(100) CreatedBy "DEFAULT 'system'"
        datetime2 UpdatedAt "NULL"
        nvarchar(100) UpdatedBy "NULL"
        bit IsDeleted "DEFAULT 0 (Soft delete)"
        rowversion RowVersion "ROWVERSION"
    }

    SERVICE_SPECS {
        bigint ServiceId PK_FK "Shared PK / FK на Services"
        nvarchar(500) Materials "NULL"
        int WarrantyMonths "CK >= 0"
        nvarchar(max) Description "NULL"
    }

    SERVICE_IMAGES {
        bigint Id PK "IDENTITY(1,1)"
        bigint ServiceId FK
        nvarchar(500) ImageUrl
        bit IsMain "Фільтрований індекс UX_Main"
        datetime2 CreatedAt "DEFAULT SYSUTCDATETIME()"
    }

    PACKAGES {
        bigint Id PK "IDENTITY(1,1)"
        nvarchar(200) Name "UNIQUE"
        nvarchar(1000) Description "NULL"
        decimal(10_2) Price "CK > 0"
        datetime2 CreatedAt "DEFAULT SYSUTCDATETIME()"
        nvarchar(100) CreatedBy "DEFAULT 'system'"
        datetime2 UpdatedAt "NULL"
        nvarchar(100) UpdatedBy "NULL"
        bit IsDeleted "DEFAULT 0 (Soft delete)"
        rowversion RowVersion "ROWVERSION"
    }

    PACKAGE_SERVICES {
        bigint PackageId PK_FK
        bigint ServiceId PK_FK
        int SortOrder "CK >= 1"
        datetime2 AddedAt "DEFAULT SYSUTCDATETIME()"
    }
```

## Опис зв'язків та обмежень:
1. **1:1 (`Services` $\leftrightarrow$ `ServiceSpecs`):**
   - Первинний ключ `ServiceSpecs.ServiceId` є одночасно зовнішнім ключем до `Services.Id` (`Shared Primary Key`).
   - Каскадне видалення: при видаленні послуги специфікація видаляється автоматично (`ON DELETE CASCADE`).
2. **1:N (`ServiceCategories` $\rightarrow$ `Services`):**
   - Категорія може містити декілька послуг. Обмеження видалення `Restrict`, що запобігає випадковому видаленню категорії з наявними послугами.
3. **1:N (`Services` $\rightarrow$ `ServiceImages`):**
   - Унікальний фільтрований індекс `UX_ServiceImages_ServiceId_MainOnly` `WHERE IsMain = 1` гарантує, що у кожної послуги може бути щонайбільше одне головне фото.
4. **M:N (`Packages` $\leftrightarrow$ `Services` через `PackageServices`):**
   - Складений первинний ключ `(PackageId, ServiceId)`.
   - Власні атрибути зв'язку: `SortOrder` (порядок у списку робіт пакету) та `AddedAt` (дата і час додавання послуги до пакету для вибірки останніх доданих послуг).
