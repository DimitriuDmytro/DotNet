# Архітектурні рішення та прийняті припущення (Architecture Decision Records)

У цьому документі зафіксовано всі проєктні рішення, обґрунтування технічних підходів та розумні припущення, прийняті під час розробки баз даних для трьох мікросервісних контекстів детейлінг-студії.

---

## ADR-01: Вибір типів первинних ключів (Primary Keys)

### Рішення
1. **Основні сутності в SQL Server (`DetailingBookings`, `DetailingCatalog`):** використовується сурогатний ключ `BIGINT IDENTITY(1,1)`.
2. **Локальна копія довідника `Services` у `DetailingBookings`:** використовується `BIGINT NOT NULL` **без `IDENTITY`**.
3. **Залежні сутності 1:1 (`AppointmentDetails`, `ServiceSpecs`):** використовується спільний первинний ключ (`Shared Primary Key`), де `PK` одночасно є `FK` на батьківську таблицю.
4. **Проміжні таблиці M:N (`AppointmentItems`, `PackageServices`):** використовується складений первинний ключ (`Composite PK`) із двох колонок (`(AppointmentId, ServiceId)` та `(PackageId, ServiceId)`).
5. **Документи в MongoDB (`detailing_reviews`):** використовується стандартний 12-байтовий `ObjectId` (у seed-скриптах задаються детерміновані фіксовані `ObjectId` для забезпечення повної ідемпотентності).

### Обґрунтування
- **Чому `BIGINT IDENTITY`, а не `UNIQUEIDENTIFIER (GUID)` у SQL Server:**
  - Кожна база даних належить одному мікросервісу (єдина точка запису — Single Writer), тому немає потреби генерувати глобально унікальні ключі на різних вузлах без координації.
  - `BIGINT` займає 8 байтів (проти 16 байтів у `GUID`), є монотонно зростаючим, що запобігає фрагментації кластерних індексів (`Clustered Index` у SQL Server за замовчуванням створюється по `PK`) і прискорює операції `INSERT` та `JOIN`.
- **Чому `Services.Id` у `DetailingBookings` без `IDENTITY`:**
  - Джерелом істини (System of Record) для послуг є сервіс `Catalog`. Коли `Catalog` створює послугу з `Id = 7`, сервіс `Bookings` має зберегти її у своїй локальній репліці саме з `Id = 7`, щоб обидва сервіси оперували однаковим ідентифікатором.

---

## ADR-02: Стратегія каскадного видалення (`ON DELETE CASCADE` vs `NO ACTION` + Soft Delete)

### Рішення
1. **`ON DELETE CASCADE` увімкнено виключно для внутрішніх частин агрегата:**
   - У `DetailingBookings`: `AppointmentDetails` (`FK_AppointmentDetails_Appointments`) та `AppointmentItems` (`FK_AppointmentItems_Appointments`).
   - У `DetailingCatalog`: `ServiceSpecs` (`FK_ServiceSpecs_Services`), `ServiceImages` (`FK_ServiceImages_Services`) та `PackageServices` (`FK_PackageServices_Packages`).
2. **`ON DELETE NO ACTION` (заборона каскаду) встановлено для коренів агрегатів та фінансово-облікових даних:**
   - Видалення клієнта (`Clients`) **не** видаляє його записи (`Appointments`).
   - Видалення запису (`Appointments`) **не** видаляє автоматично фінансові транзакції (`Payments`) чи історію статусів (`AppointmentStatusHistory`), аби унеможливити випадкову втрату фінансового аудиту.
   - Для клієнтів (`Clients`), записів (`Appointments`), послуг (`Services`) та пакетів (`Packages`) реалізовано м'яке видалення через прапорець `IsDeleted BIT NOT NULL DEFAULT 0`.

### Обґрунтування
- Деталі авто (`AppointmentDetails`) та рядки замовлення (`AppointmentItems`) не мають сенсу існування поза межами свого запису (`Appointment`). Якщо тестовий або чернетковий запис фізично видаляється, його позиції та деталі прибираються автоматично.
- Натомість клієнт та історія оплат у реальній системі ніколи не видаляються фізично: використовується `Soft Delete` (`IsDeleted = 1`), а для швидкої вибірки активних записів створено фільтрований індекс `IX_Appointments_ClientId_Active WHERE IsDeleted = 0`.

---

## ADR-03: Керування конкурентним доступом (Optimistic & Pessimistic Concurrency)

### Рішення
1. **Оптимістичне блокування (`ROWVERSION`):**
   - До таблиць `Appointments` (у `DetailingBookings`) та `Services`, `Packages` (у `DetailingCatalog`) додано колонку `RowVersion ROWVERSION NOT NULL`.
   - При читанні сутності клієнтський застосунок (Dapper або EF Core `[Timestamp]`) отримує поточне 8-байтове значення `RowVersion`, а при оновленні передає його у `WHERE Id = @Id AND RowVersion = @ExpectedRowVersion`. Якщо рядок був змінений іншим адміністратором, `@@ROWCOUNT = 0`, і застосунок сигналізує про конфлікт (`DbUpdateConcurrencyException`).
2. **Песимістичне блокування в критичних транзакціях (`WITH (UPDLOCK, ROWLOCK)`):**
   - У збережених процедурах `usp_ChangeStatus`, `usp_RegisterPayment`, `usp_DeliverCar`, `usp_CancelAppointment` читання поточного стану запису виконується всередині транзакції з табличним хінтами `WITH (UPDLOCK, ROWLOCK)`.
   - Це гарантує, що два паралельні запити (наприклад, одночасна реєстрація двох платежів або одночасна зміна статусу двома адміністраторами) не прочитають застарілий стан і не порушать бізнес-інваріанти (переплата понад `TotalAmount` або пропуск етапу статусів).

---

## ADR-04: Коди користувацьких помилок у збережених процедурах (`THROW`)

### Рішення
Усі збережені процедури `DetailingBookings` використовують `SET NOCOUNT ON; SET XACT_ABORT ON;` та єдину систему кодів помилок для оператора `THROW`:
- `50001` — Запис (`Appointment`) або клієнт (`Client`) не знайдено (або позначено як `IsDeleted = 1`).
- `50002` — Недопустимий перехід між статусами замовлення (порушення графа переходів статусів).
- `50003` — Помилка валідації оплати (сума платежу перевищує залишок до сплати за `TotalAmount` або некоректна сума).
- `50004` — Порушення умов видачі автомобіля (`usp_DeliverCar`: статус не `Ready` або сума сплачених коштів менша за `TotalAmount`).
- `50005` — Помилка вхідних даних при створенні запису (порожній список послуг у TVP або вказано неіснуючу послугу).

---

## ADR-05: Прийняті розумні припущення (Assumptions)

1. **Формат розрахунку вартості позиції (`AppointmentItems`):**
   - У таблиці `AppointmentItems` поле `Discount` трактується як абсолютна сума знижки в гривнях на відповідний рядок замовлення (`Discount DECIMAL(18,2) NOT NULL DEFAULT 0 CHECK (Discount >= 0)`), при цьому додано перевірку `CHECK (Discount <= UnitPrice * Quantity)`.
   - Підсумкова вартість запису обчислюється як: `TotalAmount = SUM(UnitPrice * Quantity - Discount)`.
2. **Таблиця дозволених переходів статусів (`usp_ChangeStatus`):**
   - Життєвий цикл замовлення суворо лінійний для основної гілки:
     - `Booked` $\rightarrow$ `CarReceived`
     - `CarReceived` $\rightarrow$ `InProgress`
     - `InProgress` $\rightarrow$ `QualityCheck`
     - `QualityCheck` $\rightarrow$ `Ready`
     - `Ready` $\rightarrow$ `Delivered`
   - Перехід у `Cancelled` дозволений **виключно** зі статусів `Booked` та `CarReceived`.
   - Процедури `usp_DeliverCar` та `usp_CancelAppointment` делегують або виконують ті самі атомарні перевірки й обов'язково записують подію в `AppointmentStatusHistory`.
3. **Сумісність схеми `DetailingCatalog` із EF Core Code First:**
   - Таблиці названо у множині (`ServiceCategories`, `Services`, `ServiceSpecs`, `ServiceImages`, `Packages`, `PackageServices`), що відповідає стандартним конвенціям `DbSet<T>` в EF Core, тоді як доменні сутності в C# матимуть назви в однині (`ServiceCategory`, `Service`, `ServiceSpec`, `ServiceImage`, `Package`, `PackageService`).
   - Усі `DATETIME2` колонки використовують UTC за замовчуванням (`SYSUTCDATETIME()`), грошові поля мають тип `DECIMAL(18,2)`, текстові поля — `NVARCHAR` для повної підтримки української мови (Unicode).
4. **Узгодження ідентифікаторів між трьома базами даних:**
   - Усі seed-скрипти (`db/p1/seed.sql`, `db/p2/seed.sql`, `db/p3/seed.js`) використовують наскрізно узгоджені `ServiceId` (`1..12`, де `1..10` присутні в локальній копії `DetailingBookings`) та `AppointmentId` (`1..8`), щоб під час демонстрації та перевірки дані логічно відповідали одне одному без жодного фізичного міжбазового зв'язку.
5. **Безпека в `docker-compose.yml`:**
   - Згідно з правилом «Жодних секретів у репозиторії», пароль адміністратора SQL Server не захардкоджений у `docker-compose.yml`, а зчитується зі змінної оточення `${MSSQL_SA_PASSWORD}`.
