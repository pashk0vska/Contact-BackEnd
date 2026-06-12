# Контакт — Web API (бекенд)

REST API сервера CRM «Контакт» на .NET 8. Обслуговує десктоп-клієнт
(Electron, репозиторій `Contact-FrontEnd`) та модуль Конфігуратора ПК і
працює зі спільною базою MySQL. Це серверна частина проєкту.

## Можливості

- **Автентифікація** — JWT із ролями `superadmin` / `admin` / `master` і розмежуванням доступу.
- **Дані** — клієнти, ремонти, продажі (з позиціями), послуги, майстри.
- **Аналітика** — агреговані звіти з експортом у PDF та Excel.
- **Документи** — друк чеків і актів виконаних робіт у PDF.
- **Адміністрування** — резервне копіювання/відновлення БД, перевірка стану системи.

## Стек

- .NET 8 Web API
- Entity Framework Core (провайдер Pomelo для MySQL/MariaDB)
- JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`)
- BCrypt.Net — хешування паролів
- QuestPDF — генерація PDF; ClosedXML — експорт Excel
- Swagger (Swashbuckle) — документація й тестування API

## База даних

MySQL/MariaDB. Схема створюється автоматично під час старту
(`EnsureCreated` + ідемпотентний `EnsureColumn`), EF-міграції не
використовуються. На старті за потреби сідиться обліковий запис `superadmin`.

## Конфігурація та секрети

Реальні значення передаються **лише через змінні середовища** — у
`appsettings.json` лежать тільки плейсхолдери:

- `CONNECTION_STRING` — рядок підключення до MySQL
- `Jwt__Key` — секрет для підпису JWT
- `PORT` — порт, який задає хостинг (Railway)

## Запуск локально

Потрібні .NET 8 SDK і доступна база MySQL.

```bash
cd ContactAPI
# задайте CONNECTION_STRING та Jwt__Key через env або appsettings.Development.json
dotnet run
```

Swagger буде доступний за адресою `/swagger`.

## Розгортання

`ContactAPI/Dockerfile` збирає та запускає сервіс у контейнері (хоститься на
Railway; конфігурація — через змінні середовища).

## Тести

```bash
dotnet test
```

Проєкт `Contact.API.Tests` (xUnit) покриває хешування паролів і базові
сценарії контролера клієнтів.

## Структура

```
ContactAPI/
  Controllers/   — ендпоінти API (Auth, Clients, Repairs, Sales, Analytics, …)
  Models/        — сутності даних
  Data/          — AppDbContext (EF Core)
  Helpers/       — звіти (PDF/Excel), хешування, резолвери, утиліти
  Program.cs     — конфігурація застосунку, ініціалізація БД, сідинг
Contact.API.Tests/ — модульні тести (xUnit)
```
