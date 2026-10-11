# Політика дублювання даних між мікросервісними контекстами

У мікросервісній архітектурі за патерном **Database per Service** прямі міжбазові `JOIN` та зовнішні ключі (`FOREIGN KEY`) між різними базами даних заборонені. Щоб кожен сервіс міг виконувати свої запити автономно і зберігати історичну достовірність даних, використовується контрольована денормалізація з чітким розмежуванням на **знімок (Snapshot)**, **репліку для читання (Read-Model Copy)** та **логічне посилання за ідентифікатором (ID Reference)**.

---

## Зведена таблиця політики дублювання

| Поле | Де зберігається | Джерело | Політика |
| :--- | :--- | :--- | :--- |
| `AppointmentItems.ServiceName, UnitPrice` | БД Записів (`DetailingBookings`) | `Service` (Каталог `DetailingCatalog`) | знімок на момент запису, не оновлюється |
| `Services.Name, Price, DurationMin` | БД Записів (`DetailingBookings`) | `Service` (Каталог `DetailingCatalog`) | копія для читання, оновлюється подіями з Каталогу |
| `reviews.serviceName` | MongoDB (`detailing_reviews`) | `Service` (Каталог `DetailingCatalog`) | копія, оновлюється подіями з Каталогу |
| `discussions.targetName` | MongoDB (`detailing_reviews`) | `Service` / `Package` (Каталог `DetailingCatalog`) | копія, оновлюється подіями з Каталогу |
| `reviews.verifiedVisit` | MongoDB (`detailing_reviews`) | `Appointments` (БД Записів `DetailingBookings`) | знімок на момент публікації відгуку |
| `reviews.author.fullName, carInfo` | MongoDB (`detailing_reviews`) | `Clients`, `AppointmentDetails` (БД Записів `DetailingBookings`) | знімок на момент публікації відгуку |
| `workReports.serviceName` | MongoDB (`detailing_reviews`) | `Service` (Каталог `DetailingCatalog`) | знімок на момент звіту |
| `workReports.carPlateNumber` | MongoDB (`detailing_reviews`) | `AppointmentDetails` (БД Записів `DetailingBookings`) | знімок на момент звіту |
| `workReports.appointmentId, reviews.appointmentId, reviews.author.clientId` | MongoDB (`detailing_reviews`) | `Appointments`, `Clients` (БД Записів `DetailingBookings`) | лише ідентифікатор, без FK |
| `AppointmentItems.ServiceId, reviews.serviceId, workReports.serviceId, discussions.targetId` | БД Записів (`DetailingBookings`), MongoDB (`detailing_reviews`) | `Service`, `Package` (Каталог `DetailingCatalog`) | лише ідентифікатор, без FK |

---

## Детальні правила синхронізації та життєвого циклу дубльованих полів

### 1. Знімки стану (Snapshots — незмінні історичні дані)
* **`AppointmentItems.ServiceName` та `AppointmentItems.UnitPrice` (`DetailingBookings`):**
  * Під час виклику процедури `usp_CreateAppointment` назва та актуальна ціна послуги копіюються з локальної таблиці `Services` у позицію замовлення `AppointmentItems`.
  * Якщо завтра в Каталозі ціна на «Керамічне покриття кузова 9H» зросте з `12000.00` до `14500.00` грн, уже оформлені та історичні записи клієнтів **не повинні змінюватися**. Фінансова звітність та домовленість із клієнтом залишаються незмінними.
* **`reviews.verifiedVisit` (`detailing_reviews`):**
  * Під час створення відгуку сервіс перевіряє, чи справді клієнт мав завершений візит (`Status = 'Delivered'`) за вказаним `appointmentId`. Результат фіксується як булевий прапорець у документі відгуку і не потребує повторних звернень до БД Записів при кожному читанні сторінки відгуків.
* **`workReports.serviceName` (`detailing_reviews`):**
  * Технічний звіт майстра є незмінним юридичним/операційним документом виконаного наряду, тому назва послуги фіксується на момент складання акта.

### 2. Копії для читання (Read-Model Copies — оновлювані за подіями)
* **`Services.Name, Price, DurationMin` (`DetailingBookings`):**
  * Таблиця `Services` у базі `DetailingBookings` має первинний ключ `Id BIGINT NOT NULL` **без `IDENTITY`**, оскільки джерелом істини (System of Record) для послуг є база `DetailingCatalog`.
  * Коли адміністратор змінює прайс або додає нову послугу в `DetailingCatalog`, сервіс Каталогу публікує інтеграційну подію (наприклад, `ServiceCreatedEvent` / `ServiceUpdatedEvent` через брокер повідомлень RabbitMQ / Kafka), а сервіс Записів оновлює свою локальну таблицю `Services`.
* **`reviews.serviceName` (`detailing_reviews`):**
  * Зберігається всередині документа відгуку, щоб при відображенні списку відгуків на UI не робити додатковий запит до сервісу Каталогу. При перейменуванні послуги в Каталозі асинхронний обробник подій виконує `db.reviews.updateMany({ serviceId: X }, { $set: { serviceName: newName } })`.

### 3. Логічні ідентифікатори без FK (ID References)
* **`AppointmentItems.ServiceId`, `reviews.serviceId`, `workReports.serviceId`, `discussions.targetId`:**
  * Зберігають числовий `BIGINT` (`NumberLong` / `Int32` у MongoDB) ідентифікатор послуги з `DetailingCatalog`.
* **`reviews.appointmentId`, `workReports.appointmentId`:**
  * Зберігають числовий ідентифікатор запису з `DetailingBookings` без будь-яких міжбазових обмежень цілісності на рівні СКБД.
