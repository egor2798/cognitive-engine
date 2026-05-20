# Cognitive Engine API + Web panel

Этот пакет добавляет к текущему API нормальную демонстрационную web-панель и расширяет backend так, чтобы было видно не только `body-points`, но и упражнения, сессии, samples, events и metrics.

## Что добавлено

### Backend

Новые контроллеры:

- `GET /api/dashboard` — сводка по БД: точки тела, пациенты, упражнения, сессии, samples, events, metrics.
- `GET /api/body-points` — справочник 11 точек тела.
- `GET /api/exercise-templates` — шаблоны упражнений.
- `GET /api/exercises` — назначенные упражнения.
- `GET /api/sessions` — список сессий с основными метриками.
- `GET /api/sessions/{id}` — подробности сессии: metrics, events, samples.
- `POST /api/sessions/import-json` — импорт `session_result.json` из Unity.
- `POST /api/demo/seed-session` — создание демо-сессии для показа.

### Frontend

Добавлена простая web-панель:

- `http://localhost:5000/`

На ней отображается:

- сводка по БД;
- справочник точек тела;
- упражнения;
- сессии и метрики;
- импорт Unity `session_result.json`;
- кнопка создания демо-сессии.

Swagger остаётся доступен:

- `http://localhost:5000/swagger`

## Куда класть файлы

Если заменяешь весь проект — просто используй содержимое этого архива как папку API-проекта.

Если добавляешь вручную:

```text
Controllers/BodyPointsController.cs
Controllers/DashboardController.cs
Controllers/ExercisesController.cs
Controllers/SessionsController.cs
Dtos/ApiDtos.cs
wwwroot/index.html
wwwroot/styles.css
wwwroot/app.js
Program.cs
```

`Program.cs` уже поправлен: включены CORS и раздача статического сайта из `wwwroot`.

## Запуск

1. Подними PostgreSQL через Docker:

```bash
docker compose up -d
```

2. Убедись, что БД создана и seed накатился.

3. Запусти API:

```bash
dotnet run
```

4. Открой:

```text
http://localhost:5000/
```

или Swagger:

```text
http://localhost:5000/swagger
```

## Проверка

1. Открой сайт `http://localhost:5000/`.
2. В блоке “Справочник точек тела” должны появиться 11 точек тела.
3. Нажми “Создать демо-сессию”.
4. В блоке “Сессии и метрики” должна появиться сессия с метриками:
   - meanDeviationMm;
   - maxDeviationMm;
   - rmseDeviationMm;
   - timeOutsideSec;
   - meanPointerSpeedMmS;
   - pacemakerPathLengthMm.
5. Можно загрузить настоящий `session_result.json` из Unity через кнопку импорта.

## Что можно сказать заказчику

В проект добавлен начальный web/API-контур: данные теперь не только сохраняются локально в JSON, но и могут загружаться в серверную часть. Через API доступны справочники, упражнения, сессии, события, samples и итоговые метрики. Web-панель показывает, что сайт подключён к API и получает данные из backend/БД.

## Важное ограничение

Это не финальный личный кабинет врача, а демонстрационная web-панель для проверки связки:

```text
БД → API → сайт → сессии/метрики
```

Для полноценной версии дальше можно расширять:

- patients;
- anthropometry;
- sensor mounts;
- reports;
- dynamics;
- authorization.
