# Деплой бекенду на Railway + збірка десктоп-клієнта

## А. Бекенд → Railway (один раз)

1. Railway → проєкт із вашою базою → **New → Service → GitHub Repo** (або Deploy from repo).
2. У налаштуваннях сервісу: **Settings → Root Directory** = шлях до цієї папки
   (де лежать `Dockerfile` і `Contact.API.csproj`). Railway сам підхопить `Dockerfile`.
3. **Variables** — додайте змінну:
   - `CONNECTION_STRING` =
     `Server=mysql.railway.internal;Port=3306;Database=railway;User=root;Password=ВАШ_НОВИЙ_ПАРОЛЬ;`
     (внутрішній хост `mysql.railway.internal` швидший; можна лишити й публічний
     `acela.proxy.rlwy.net:47992`, якщо так зручніше).
   - Порт задавати НЕ треба — Railway передає його через `PORT`, код уже це слухає.
4. **Settings → Networking → Generate Domain**. Отримаєте URL, напр.
   `https://contact-api-production.up.railway.app`.
5. Перевірка: відкрийте `https://ВАШ-URL/api/Dashboard` у браузері — має бути 401
   (а не помилка з'єднання). Це означає, що API живий.

> Безпека: пароль до БД зараз лежить у `appsettings.json` (він уже «засвітився»).
> Раджу **змінити пароль** у Railway-базі й тримати рядок підключення лише у
> змінній `CONNECTION_STRING`, а в `appsettings.json` залишити порожнім.

## Б. Десктоп-клієнт (фронтенд)

1. У файлі `renderer/core/config.js` впишіть свій Railway-URL у `window.API_BASE`
   (без слеша в кінці).
2. У папці фронтенду:
   ```
   npm install
   npm run dist
   ```
3. Готовий інсталятор з'явиться в папці `dist/`
   (`Kontakt CRM Setup 1.0.0.exe`). Подвійний клік — встановлення, ярлик на
   робочому столі. Запуск → одразу вікно логіну, бо бекенд і база вже на Railway.

> Іконку додасте пізніше: покладіть `icon.ico` (256×256) у корінь фронтенду й
> допишіть у `package.json` → `build.win.icon`.
