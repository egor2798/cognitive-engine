# Cognitive Engine — БД, версия v2 (по Документу 07)

> **Это переписанная схема под новое ТЗ.** Старая БД (категории упражнений,
> тренировки-наборы, спецтренировки, подписки/платежи) **не применима** —
> домен изменился. Здесь — программно-аппаратный комплекс с вертикальной
> панелью 100", трекерами, калибровкой, временны́ми рядами координат и
> воспроизводимыми отчётами.

---

## Что изменилось vs прежней версии

| Убрано | Заменено / Добавлено |
|---|---|
| `subscriptions`, `payments` | — (модель монетизации отсутствует в ТЗ v2) |
| `scheduled_trainings` | `exercises.scheduled_at` (одно поле) |
| `trainings`, `training_exercises` (наборы) | один `exercise_templates` с полным конфигом в JSONB |
| `training_categories` | поле `scope` (System / Organization / Personal) |
| `exercise_results` (агрегат) | разнесено в три таблицы: `session_samples` + `session_events` + `metrics` |
| — | `devices` (вертикальная панель / проекция) |
| — | `body_points` (фиксированный справочник 11 точек тела) |
| — | `trackers` (Vive / IMU / камеры) |
| — | `calibration_profiles` (физический масштаб, 4 угла, схема секций тела) |
| — | `free_traces` → `derived_tracks` (запись фактического движения → шаблон) |
| — | `audit_logs` (ранее опционально, теперь обязательно) |
| — | `idempotency_keys` (для batch-загрузки телеметрии) |

Итог: **25 таблиц**, **87 индексов**, **57 FK**, **207 CHECK-ограничений**.

---

## Ключевые архитектурные решения

### 1. Snapshot-паттерн в `sessions` (Док.06 §26.3)
При старте сессии копируем конфиг шаблона и параметры калибровки в JSONB-поля
`exercise_config_snapshot` и `calibration_snapshot`. Это даёт **воспроизводимость**
отчёта даже если шаблон или калибровочный профиль позже изменились.
Дополнительно сохраняется `config_hash` (SHA-256) — гарантия идентичности
конфига между Player и Backend.

### 2. JSONB вместо реляционной декомпозиции для конфига упражнения
Тело шаблона (`activeBodyPoints`, `bodyPointZones`, `shapes`, `tracks`,
`corridors`, `pacers`, `cursors`, `rules`, `stimuli`, `metricsConfig`,
`runtimeConfig`) хранится одним документом в `exercise_templates.config_json`.
Внутри — стабильные строковые id (`zone_right_forearm`, `track_1`, `cursor_1`),
на которые ссылаются `session_samples` и `session_events` через `*_ref` колонки.
Это удобнее, чем 10 таблиц с тонкими связями.

### 3. Горячая таблица `session_samples`
- `BIGSERIAL` id (миллионы строк).
- Композитная уникальность `(session_id, kind, sample_index)` → встроенная
  **идемпотентность batch-вставки**: повтор того же batch'а отклоняется БД
  без логики на стороне приложения.
- Индексы:
  - `(session_id, t)` — основной запрос «траектория за период»;
  - `(session_id, slot_index, t)` — для конкретного курсора 1..3;
  - **BRIN** на `t` — компактен и эффективен на монотонно возрастающем времени.
- На > 50 млн строк имеет смысл перейти на **TimescaleDB hypertable** или
  декларативное партиционирование по `session_id`. Сейчас работает на «голом»
  PostgreSQL 14+.

### 4. Иммутабельность `session_events`
События не редактируются. Коррекция = новое событие с другим `type`.
Это упрощает аудит и аналитику.

### 5. Справочник `body_points` (11 строк)
Сидится один раз при инициализации. На него ссылаются `trackers`,
`session_samples`, `free_traces`. После старта эти 11 записей считаются
иммутабельными.

---

## Запуск

### Вариант А — Docker (рекомендую)

```bash
docker compose up -d
```

Что произойдёт:
1. Поднимется `postgres:16-alpine` на `localhost:5432`.
2. На пустом томе автоматически выполнятся `db/init.sql` и `db/seed.sql`.
3. Поднимется pgAdmin на http://localhost:5050 (`admin@local` / `admin`).

Запустить API:
```bash
cd backend/CognitiveEngine.API
dotnet restore
dotnet run
```

Откроется Swagger на `https://localhost:5xxx/swagger`.

### Вариант Б — Нативный PostgreSQL

Поставить PostgreSQL 14+, затем:
```sql
CREATE USER cogengine WITH PASSWORD 'cogengine_dev_pwd';
CREATE DATABASE cognitive_engine OWNER cogengine;
```
```bash
psql -U cogengine -d cognitive_engine -f db/init.sql
psql -U cogengine -d cognitive_engine -f db/seed.sql
cd backend/CognitiveEngine.API && dotnet run
```

---

## Demo-credentials

Все 4 demo-пользователя имеют пароль `Demo@12345` (хэш — placeholder для DEV,
в проде хэш генерирует приложение через `BCrypt.HashPassword`):

| login | роль (Док.06 §25.3) |
|---|---|
| `admin` | Admin |
| `operator` | Оператор (врач) |
| `methodist` | Методист |
| `researcher` | Исследователь |

Демо-пациент: `P-0001` (Пётр Тестовый, рост 178). К нему привязаны:
- устройство «Vertical Panel 100" #1» (2160×3840 px, 1245×2214 мм);
- профиль калибровки;
- 3 трекера: Vive на правом предплечье, IMU на груди, IMU на левой голени;
- шаблон «Дуга правой рукой» (опубликован, scope=Organization);
- назначение упражнения по нему.

---

## Примеры запросов

**Шаблон ↔ активные точки тела:**
```sql
SELECT id, name, jsonb_path_query(config_json, '$.activeBodyPoints[*]') AS body_point
FROM exercise_templates;
```

**Траектория курсора 1 в сессии 42, с 5 по 15 сек:**
```sql
SELECT t, screen_x, screen_y, inside_corridor, distance_to_track_center_mm
FROM session_samples
WHERE session_id = 42 AND kind = 1 AND slot_index = 1
  AND t BETWEEN 5 AND 15
ORDER BY t;
```

**Сводка по сессии: время вне коридора, % опережения пейсмекера:**
```sql
SELECT code, value, unit
FROM metrics
WHERE session_id = 42 AND scope = 1
ORDER BY code;
```

**Идемпотентный batch-инсёрт телеметрии (на уровне БД):**
```sql
INSERT INTO session_samples (session_id, kind, sample_index, t, slot_index, signal_status, screen_x, screen_y)
VALUES (42, 1, 1001, 16.6533, 1, 1, 0.512, 0.484)
ON CONFLICT (session_id, kind, sample_index) DO NOTHING;
```

---

## Контроль качества схемы

`db/init.sql` применяется к чистой PostgreSQL 16 и проходит следующие проверки:

| # | Проверка | Ожидание |
|---|---|---|
| 1 | `INSERT INTO users (login='bad login', ...)` | ❌ `ck_users_login_format` |
| 2 | Дубль `(organization_id, code)` в `patients` | ❌ `ux_patients_org_code` |
| 3 | Дубль `(session_id, kind, sample_index)` в `session_samples` | ❌ `ux_ss_session_kind_idx` (идемпотентность) |
| 4 | Сессия с `device_id` несуществующим | ❌ `fk_se_device` |
| 5 | `session_samples.confidence = 1.5` | ❌ `ck_ss_conf` |
| 6 | `sessions.finished_at < started_at` | ❌ `ck_se_finish` |
| 7 | `DELETE FROM patients` → каскад на sessions/samples | ✅ 0 строк |
| 8 | `jsonb_path_query(config_json, '$.activeBodyPoints[*]')` | ✅ |

Все 8 — прошли. Лог приложения init.sql находится в `db/init.sql` (один файл, идемпотентен по `IF NOT EXISTS`/триггерам нет, поэтому **применять на чистой БД**).

---

## Структура файлов

```
.
├── docker-compose.yml
├── db/
│   ├── init.sql                 # 25 таблиц, 87 индексов, 57 FK, 207 CHECK
│   └── seed.sql                 # 11 точек тела + demo-данные
├── diagrams/
│   └── er-diagram.mmd           # Mermaid ER-диаграмма
└── backend/
    └── CognitiveEngine.API/
        ├── CognitiveEngine.API.csproj
        ├── Program.cs           # PostgreSQL + snake_case + CORS + Swagger
        ├── appsettings.json
        ├── appsettings.Development.json
        ├── Controllers/
        │   └── UsersController.cs  (от исходного проекта — компилируется как есть)
        └── Models/
            ├── AppDbContext.cs
            ├── Enums/DomainEnums.cs
            ├── Identity.cs           # Organization, User, Patient, Consent
            ├── Hardware.cs           # Device, Tracker, BodyPoint, CalibrationProfile
            ├── Content.cs            # ExerciseTemplate, Exercise, FreeTrace, DerivedTrack
            ├── Telemetry.cs          # Session, SessionSample, SessionEvent, Metric
            └── Reporting.cs          # Report, Export, Comment, Attachment, AuditLog, Auth, IdempotencyKey
```

---

## Что делать дальше

1. **Контроллеры** — сейчас только `UsersController`. Логично добавить:
   - `AuthController` (login/refresh/reset) с BCrypt и JWT;
   - `PatientsController`, `DevicesController`, `CalibrationController`;
   - `ExerciseTemplatesController` с публикацией версий;
   - `SessionsController` с batch-эндпоинтом `POST /sessions/{id}/samples/batch`
     (используя таблицу `idempotency_keys`).
2. **DTO** — модели сейчас сериализуются напрямую (это нормально для каркаса, но
   в проде лучше DTO + AutoMapper).
3. **Миграции** — если перейдёте на EF migrations, `init.sql` останется как
   референс/быстрая инициализация для dev/test.
4. **Метрики** — реализовать расчёт `metrics.code` в фоновой job после `session.completed`.
5. **Партиционирование `session_samples`** — когда подойдёте к 50 млн строк.
