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

---

## ADR-06: Вибір платформи .NET 9 SDK та формату рішення `.slnx` (Лаб. 02)

### Рішення
- Усі проєкти рішення (`Bookings.Domain`, `Bookings.Dal`, `Bookings.Bll`, `Bookings.Api`, `Bookings.Tests`, `Platform.AppHost`) зібрані під цільову платформу **`net9.0`** (SDK `9.0.318`).
- Використано сучасний XML-формат файлу рішення **`Platform.slnx`** у корені репозиторію.
- Для оркестрації використано **.NET Aspire 9.1.0** (`Aspire.AppHost.Sdk/9.1.0`, `Aspire.Hosting.AppHost`, `Aspire.Hosting.SqlServer`).

### Обґрунтування
- Формат `.slnx` є лаконічним, людиночитаним XML без надлишкових GUID-ідентифікаторів конфігурацій, що мінімізує конфлікти злиття в Git.
- Назви проєктів та шарів суворо відповідають тришаровій архітектурі: `Bookings.Api` $\rightarrow$ `Bookings.Bll` $\rightarrow$ `Bookings.Dal` (+ наскрізний `Bookings.Domain` без зовнішніх залежностей).

---

## ADR-07: Поєднання чистого ADO.NET та Dapper у шарі DAL

### Рішення
1. **`ClientRepository` (`IClientRepository`):** реалізований **виключно на чистому ADO.NET** (`SqlCommand`, `SqlParameter` з явним типом `SqlDbType` і розміром, ручне читання `SqlDataReader`). Інтерфейс `IClientRepository` навмисно **не** успадковує `IGenericRepository<T>`, щоб продемонструвати низькорівневий спеціалізований контракт ADO.NET (пошук за Email/Phone, перевірка існування клієнта за контактами перед створенням без завантаження всіх записів у пам'ять).
2. **`BaseDapperRepository<T>` (`IGenericRepository<T> where T : class`):** узагальнений інтерфейс `IGenericRepository<T>` напряму оголошує 5 базових асинхронних методів (`GetByIdAsync`, `GetAllAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync` із `CancellationToken`), а базовий клас `BaseDapperRepository<T>` реалізує їх та м'яке видалення (`IsDeleted = 1`) за допомогою мікро-ORM **Dapper**.
3. **`AppointmentRepository`, `PaymentRepository`, `CatalogServiceRepository`:** успадковують `IGenericRepository<T>` та `BaseDapperRepository<T>` без дублювання базових CRUD-методів, додаючи лише доменно-специфічні операції. Повернення результатів складених операцій оформлено через іменовані доменні моделі (`CreateAppointmentResult`) замість кортежів (`ValueTuple`).
4. **Явне формування історичного знімка (`ServiceName`, `UnitPrice`) у BLL (`AppointmentService.CreateAsync`):** всередині транзакції `IUnitOfWork` сервіс `AppointmentService` завантажує замовлені послуги з локальної репліки `_unitOfWork.Services.GetByIdsAsync(...)`, явно копіює `ServiceName = service.Name` та `UnitPrice = service.Price` у сутності `AppointmentItem`, обчислює `TotalAmount = sum(UnitPrice * Quantity - Discount)` та передає сформовані знімки в DAL/TVP.

### Обґрунтування
- Такий підхід дозволяє порівняти трудомісткість і контроль чистого ADO.NET із продуктивністю Dapper у межах однієї бази даних і однієї транзакції.
- Весь SQL-код та імена збережених процедур повністю інкапсульовані всередині `Bookings.Dal` — у `Bookings.Bll` та `Bookings.Api` немає жодного SQL-рядка чи посилання на `Microsoft.Data.SqlClient`.

---

## ADR-08: Координація транзакцій через Unit of Work (`IUnitOfWork`), рівень ізоляції та таймаути

### Рішення
1. **Єдине з'єднання та транзакція:** Клас `UnitOfWork` створює та утримує єдиний екземпляр `SqlConnection` на час життя HTTP-запиту (`Scoped`) і передає всім репозиторіям делегат `Func<SqlTransaction?>` для доступу до поточної активної транзакції.
2. **Рівень ізоляції (`IsolationLevel.ReadCommitted`):** Метод `BeginTransactionAsync` відкриває транзакцію з рівнем ізоляції `IsolationLevel.ReadCommitted`, тоді як критичні читання стану перед оновленням у збережених процедурах (`usp_ChangeStatus`, `usp_RegisterPayment`, `usp_DeliverCar`, `usp_CancelAppointment`) використовують точкові блокувальні хінти `WITH (UPDLOCK, ROWLOCK)` (та `WITH (HOLDLOCK)` у `usp_CreateAppointment`).
3. **Таймаут виконання команд (`DefaultCommandTimeoutSeconds = 30`):** У базовому класі `RepositoryBase` встановлено константу `DefaultCommandTimeoutSeconds = 30` секунд, яка явно передається як у `SqlCommand.CommandTimeout` (чистий ADO.NET), так і в `CommandDefinition(commandTimeout: 30)` (Dapper).
4. **Безпечний відкат:** У разі будь-якого винятку блок `catch` у BLL гарантовано викликає `await _unitOfWork.RollbackAsync()`, який безпечно перевіряє, чи транзакція вже не була відкочена на боці SQL Server через `SET XACT_ABORT ON`.

### Обґрунтування (Компроміс Deadlocks vs Consistency)
- **Чому не `Serializable` або `RepeatableRead` глобально для всієї транзакції UoW:**
  - Глобальний рівень `Serializable` у SQL Server накладає діапазонні блокування (`Key-Range Locks`, `RangeS-S`), що при одночасному оформленні записів різними клієнтами призводить до частих взаємних блокувань (**Deadlocks**, код `1205`) та різкого падіння пропускної здатності (Throughput).
  - Рівень `ReadCommitted` у поєднанні з точковим `WITH (UPDLOCK, ROWLOCK)` на конкретному рядку `Appointments WHERE Id = @AppointmentId` забезпечує ідеальний баланс **Consistency vs Deadlocks**:
    1. Звичайні читання не утримують S-блокування до кінця транзакції.
    2. Два паралельні запити на зміну статусу чи оплату одного й того самого запису шикуються в чергу на `U`-блокуванні (`UPDLOCK`) від самого початку читання стану, що запобігає класичному дедлоку конверсії `S -> X` (коли дві транзакції спочатку читають рядок під `S`-lock у `RepeatableRead`, а потім обидві намагаються підвищити його до `X`-lock при `UPDATE`).
    3. Таймаут 30 секунд захищає пул потоків та пул з'єднань від «завислих» запитів при мережевих збоях або довгих блокуваннях.

---

## ADR-09: Трансляція помилок БД у доменні винятки та RFC 7807 ProblemDetails

### Рішення
1. **Рівень DAL (`RepositoryBase.TranslateSqlException`):** перехоплює `SqlException` і перетворює коди помилок SQL Server на доменні винятки з `Bookings.Domain.Exceptions` без витоку сирих інфраструктурних SQL-повідомлень:
   - `50001` $\rightarrow$ `NotFoundException`
   - `50002`, `50003`, `50004` $\rightarrow$ `BusinessConflictException`
   - `50005` $\rightarrow$ `ValidationException`
   - `2601`, `2627` (порушення `UNIQUE`) $\rightarrow$ `BusinessConflictException`
   - `547` (порушення `FOREIGN KEY` / `CHECK`) $\rightarrow$ `BusinessConflictException`
   - `1205` (Deadlock) $\rightarrow$ `BusinessConflictException`
   - `-2` (Timeout) $\rightarrow$ `TimeoutException`
2. **Рівень API (`DomainExceptionHandler : IExceptionHandler`):** перехоплює винятки на рівні всього пайплайну ASP.NET Core і формує відповідь у форматі **RFC 7807 `ProblemDetails`** (`application/problem+json`) з обов'язковим полем `traceId` у `Extensions`:
   - `ValidationException` $\rightarrow$ `400 Bad Request`
   - `NotFoundException` $\rightarrow$ `404 Not Found`
   - `BusinessConflictException` $\rightarrow$ `409 Conflict`
   - Інші винятки $\rightarrow$ `500 Internal Server Error` (де в `detail` повертається безпечне загальне повідомлення без витоку `exception.Message`).
3. Контролери не містять блоків `try/catch` і залишаються тонкими.
