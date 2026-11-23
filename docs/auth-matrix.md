# Матриця доступу (Authentication & Authorization Matrix) — Лабораторна робота №09

Цей документ описує рольову модель, необхідні дозволи (permissions / scopes) та матрицю доступу до ендпоінтів мікросервісної платформи детейлінг-студії згідно з вимогами Практичного заняття №09.

---

## 1. Рольова модель

| Роль | Призначення | Опис |
| :--- | :--- | :--- |
| **`admin`** | Головний адміністратор платформи | Повний доступ до всіх ресурсів, включаючи платежі, управління користувачами, видалення та схвалення. |
| **`manager`** | Менеджер детейлінг-студії | Доступ до перегляду, створення, оновлення та схвалення замовлень, оновлення каталогу послуг. |
| **`user`** | Зареєстрований клієнт | Доступ до перегляду та створення власних замовлень, публічного каталогу послуг та відгуків. |
| **`guest`** | Анонімний / гостьовий користувач | Доступ тільки до читання публічного каталогу послуг та відгуків. |

---

## 2. Матриця дозволів для бізнес-ендпоінтів (Таблиця 7 методички)

| Мікросервіс | Метод & Маршрут | Необхідний Permission (`[RequirePermission]`) | `admin` | `manager` | `user` | `guest` |
| :--- | :--- | :--- | :---: | :---: | :---: | :---: |
| **Orders (Bookings)** | `GET /api/orders`, `GET /api/orders/{id}` | `orders:read` | ✅ | ✅ | ✅ | ❌ |
| | `POST /api/orders` | `orders:create` | ✅ | ✅ | ✅ | ❌ |
| | `PUT /api/orders/{id}`, `PATCH /api/orders/{id}/status` | `orders:update` | ✅ | ✅ | ❌ | ❌ |
| | `DELETE /api/orders/{id}`, `POST /api/orders/{id}/cancel` | `orders:delete` | ✅ | ❌ | ❌ | ❌ |
| | `POST /api/orders/{id}/approve`, `POST /api/orders/{id}/deliver` | `orders:approve` | ✅ | ✅ | ❌ | ❌ |
| **Products (Catalog)** | `GET /api/products`, `GET /api/products/{id}` | `products:read` | ✅ | ✅ | ✅ | ✅ |
| | `POST /api/products` | `products:create` | ✅ | ✅ | ❌ | ❌ |
| | `PUT /api/products/{id}`, `PATCH /api/products/{id}/price` | `products:update` | ✅ | ✅ | ❌ | ❌ |
| | `DELETE /api/products/{id}` | `products:delete` | ✅ | ❌ | ❌ | ❌ |
| | `POST /api/products/{id}/publish` | `products:publish` | ✅ | ❌ | ❌ | ❌ |
| **Payments** | `GET /api/payments` | `payments:read` | ✅ | ❌ | ❌ | ❌ |
| | `POST /api/payments/process` | `payments:process` | ✅ | ❌ | ❌ | ❌ |
| | `POST /api/payments/refund` | `payments:refund` | ✅ | ❌ | ❌ | ❌ |
| **Reviews** | `GET /api/reviews` | `[Authorize]` / Public Read | ✅ | ✅ | ✅ | ✅ |
| | `POST /api/reviews` | `[Authorize]` | ✅ | ✅ | ✅ | ❌ |

> **Важливо (Payments Security):** Згідно з Таблицею 7 вимог лабораторної роботи, сервіс `Payments.Api` є критичним фінансовим доменом: доступ до `payments:read`, `payments:process` та `payments:refund` дозволено **ВИКЛЮЧНО** користувачам з роллю `admin`. Будь-які спроби запитів з ролями `manager`, `user` або `guest` повертають HTTP `403 Forbidden`.

---

## 3. Матриця доступу до сервісу автентифікації та користувачів (Auth.Api)

| Метод & Маршрут | Призначення | Авторизація / Правило доступу |
| :--- | :--- | :--- |
| `POST /auth/register` | Реєстрація нового користувача | Публічний (роль за замовчуванням `User`) |
| `POST /auth/login` | Вхід за паролем | Публічний (перевірка `EmailConfirmed` та Lockout) |
| `POST /auth/refresh-token` | Ротація refresh-токена | Публічний (токен у cookie або body) |
| `POST /auth/revoke-token` | Відкликання сесії | `[Authorize]` |
| `POST /auth/logout` | Вихід та очищення cookie | `[Authorize]` |
| `POST /auth/verify-email` | Підтвердження електронної пошти | Публічний (за одноразовим токеном) |
| `POST /auth/resend-verification` | Повторна відправка листа | Публічний |
| `POST /auth/forgot-password` | Запит на відновлення пароля | Публічний (безпечна відповідь без витоку існування email) |
| `POST /auth/reset-password` | Скидання пароля | Публічний (відкликає ВСІ активні refresh-токени користувача) |
| `POST /auth/change-password` | Зміна поточного пароля | `[Authorize]` |
| `GET /users` | Список усіх користувачів | Тільки `Admin` |
| `GET /users/{id}` | Отримання профілю користувача | Власник (`sub == id`) або `Admin` |
| `PUT /users/{id}` | Оновлення даних профілю | Власник (`sub == id`) або `Admin` |
| `DELETE /users/{id}` | Видалення облікового запису | Тільки `Admin` |
| `GET /users/{id}/refresh-tokens`| Активні сесії користувача | Власник (`sub == id`) або `Admin` |
| `GET /users/{id}/roles` | Ролі зазначеного користувача | Аутентифікований користувач |
| `POST /users/{id}/roles` | Призначення ролі користувачу | Тільки `Admin` |
| `DELETE /users/{id}/roles/{role}` | Зняття ролі з користувача | Тільки `Admin` |

---

## 4. Клієнти OAuth 2.0 / OpenID Connect

| Клієнт | Тип гранту | Секрет / PKCE | Дозволені Scopes | Цільове призначення |
| :--- | :--- | :--- | :--- | :--- |
| **`postman`** | `authorization_code` | `postman-secret` | `openid`, `profile`, `orders.*`, `products.*`, `payments.*` | Інтерактивне тестування API через Postman |
| **`swagger`** | `authorization_code` + PKCE | Публічний (`RequireClientSecret = false`, `RequirePkce = true`) | `openid`, `profile`, `orders.*`, `products.*`, `payments.*` | Авторизація через Swagger UI у браузері |
| **`aggregator_service`** | `client_credentials` | `aggregator-secret` | `orders.read`, `products.read` | M2M службова взаємодія Aggregator з мікросервісами (без `sub`) |

---

## 5. Тестові облікові записи (Keycloak & IdentityServer)

| Користувач | Email | Пароль | Призначена роль | Рівень доступу |
| :--- | :--- | :--- | :--- | :--- |
| **`alice`** | `alice@dev.local` | `Alice123!Secure` | `admin` | Повний доступ (Orders, Products, Payments, Users) |
| **`john.manager`** | `john.manager@dev.local` | `Manager123!Secure` | `manager` | Схвалення замовлень, керування каталогом |
| **`bob`** | `bob@dev.local` | `Bob123!Secure` | `user` | Створення та перегляд замовлень, каталог |
| **`guest`** | `guest@dev.local` | `Guest123!Secure` | `guest` | Тільки публічний перегляд каталогу |
| **`admin`** (Auth.Api) | `admin@platform.local` | `Admin123!Secure` | `Admin` | Повний доступ до ендпоінтів `/users` |
