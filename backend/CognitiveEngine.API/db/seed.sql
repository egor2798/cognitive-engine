-- ============================================================================
-- Cognitive Engine — seed (Документ 07)
-- Применять ПОСЛЕ init.sql
--   psql -d cognitive_engine -f db/seed.sql
-- ============================================================================

BEGIN;

-- ----------------------------------------------------------------------------
-- 1. Справочник 11 точек тела (Док.02 §5.1) — CRITICAL, на эти id ссылается всё.
-- ----------------------------------------------------------------------------
INSERT INTO body_points (code, name, side, "group", order_index) VALUES
    ('BP-01', 'Голова',                         1, 1, 1),   -- central_head
    ('BP-02', 'Грудь',                          1, 2, 2),   -- central_chest
    ('BP-03', 'Корпус / живот',                 1, 3, 3),   -- central_body
    ('BP-04', 'Правое плечо',                   3, 4, 4),   -- right_arm
    ('BP-05', 'Правое предплечье / кисть',      3, 4, 5),
    ('BP-06', 'Правое бедро',                   3, 6, 6),   -- right_leg
    ('BP-07', 'Правая голень',                  3, 6, 7),
    ('BP-08', 'Левое плечо',                    2, 5, 8),   -- left_arm
    ('BP-09', 'Левое предплечье / кисть',       2, 5, 9),
    ('BP-10', 'Левое бедро',                    2, 7, 10),  -- left_leg
    ('BP-11', 'Левая голень',                   2, 7, 11);


-- ----------------------------------------------------------------------------
-- 2. Демо-организация и пользователи
-- ----------------------------------------------------------------------------
INSERT INTO organizations (name, type, inn, address, phone, email) VALUES
    ('Test Clinic', 3, '7700000000', 'Москва, ул. Тестовая, 1', '+7 (495) 000-00-00', 'info@test-clinic.ru');

-- BCrypt-хэш пароля "Demo@12345" (cost 11). Для DEV. В проде хэш генерит приложение.
INSERT INTO users (login, email, first_name, last_name, password_hash, role, organization_id) VALUES
    ('admin',      'admin@test.com',    'Алексей', 'Админов',         '$2a$11$abcdefghijklmnopqrstuOWmNBy.7nN0DUDIGyA7xv0p/y0fvKiTVu', 4, 1),
    ('operator',   'operator@test.com', 'Ольга',   'Операторова',     '$2a$11$abcdefghijklmnopqrstuOWmNBy.7nN0DUDIGyA7xv0p/y0fvKiTVu', 1, 1),
    ('methodist',  'method@test.com',   'Мария',   'Методистова',     '$2a$11$abcdefghijklmnopqrstuOWmNBy.7nN0DUDIGyA7xv0p/y0fvKiTVu', 3, 1),
    ('researcher', 'research@test.com', 'Иван',    'Исследовательев', '$2a$11$abcdefghijklmnopqrstuOWmNBy.7nN0DUDIGyA7xv0p/y0fvKiTVu', 5, 1);


-- ----------------------------------------------------------------------------
-- 3. Тестовый испытуемый (с кодом для деперсонализации)
-- ----------------------------------------------------------------------------
INSERT INTO patients (code, first_name, last_name, middle_name, birth_date, gender, height_cm, weight_kg, restrictions, organization_id, profile_data) VALUES
    ('P-0001', 'Пётр', 'Тестовый', 'Иванович', '1990-01-15', 1, 178.0, 75.0,
     'Поднимать правую руку только до уровня плеча; нет противопоказаний к движениям ног',
     1,
     '{"diagnosis":"ЦВБ","secondary":"","comments":""}'::jsonb);

INSERT INTO consents (patient_id, type, granted, agreement_version) VALUES (1, 1, TRUE, '1.0');


-- ----------------------------------------------------------------------------
-- 4. Устройство (вертикальная панель 100") + калибровочный профиль
-- ----------------------------------------------------------------------------
INSERT INTO devices (name, type, location, serial_number, width_px, height_px, is_portrait,
                     physical_width_mm, physical_height_mm, status, organization_id) VALUES
    ('Vertical Panel 100" #1', 1, 'Кабинет 305', 'CE-VP100-001',
     2160, 3840, TRUE,            -- portrait: 2160 wide × 3840 tall
     1245.00, 2214.00,             -- ~100" diag at 16:9 в портретной ориентации
     1, 1);

INSERT INTO calibration_profiles (
    name, device_id, mode, patient_id,
    width_px, height_px, physical_width_mm, physical_height_mm,
    scale_x, scale_y, offset_x_mm, offset_y_mm, patient_height_cm,
    config_json
) VALUES (
    'Профиль панели #1, рост 178 см', 1, 1, 1,
    2160, 3840, 1245.00, 2214.00,
    0.57639, 0.57656,                        -- mm на px
    0, 0, 178.0,
    -- 4 угла + базовая разметка секций тела + соответствие trackerId↔bodyPoint + IMU zero pose
    '{
       "corners": {"tl":[0,0],"tr":[1,0],"br":[1,1],"bl":[0,1]},
       "bodySectionLayout": {
         "central":      {"x":0.34,"y":0.05,"w":0.32,"h":0.55},
         "right_arm":    {"x":0.66,"y":0.05,"w":0.34,"h":0.45},
         "left_arm":     {"x":0.00,"y":0.05,"w":0.34,"h":0.45},
         "right_leg":    {"x":0.55,"y":0.55,"w":0.45,"h":0.45},
         "left_leg":     {"x":0.00,"y":0.55,"w":0.45,"h":0.45}
       },
       "trackerMapping": [],
       "imuZeroPose": {"quaternion":[0,0,0,1]},
       "filterParams": {"smoothing":0.2,"outlierThresholdMm":50}
     }'::jsonb
);

UPDATE devices SET active_calibration_profile_id = 1 WHERE id = 1;


-- ----------------------------------------------------------------------------
-- 5. Демо-трекеры (Vive + IMU). Закрепляем за правым предплечьем, грудью, левой голенью.
-- ----------------------------------------------------------------------------
INSERT INTO trackers (name, type, serial_number, device_id, body_point_id, status) VALUES
    ('Vive RF #1',  1, 'VIVE-001', 1, 5,  1),    -- правое предплечье (BP-05)
    ('IMU #2',      2, 'IMU-002',  1, 2,  1),    -- грудь            (BP-02)
    ('IMU #3',      2, 'IMU-003',  1, 11, 1);    -- левая голень     (BP-11)


-- ----------------------------------------------------------------------------
-- 6. Демо-шаблон упражнения (TemplateScope=Organization, опубликован).
--    Конфиг — пример полной структуры (Док.06 §26.4).
-- ----------------------------------------------------------------------------
INSERT INTO exercise_templates (
    name, description, scope, status, version, organization_id,
    created_by_id, published_by_id, published_at, config_json, config_hash
) VALUES (
    'Дуга правой рукой',
    'Базовое упражнение: ведение целеуказателя по эталонному треку-дуге в секции правой руки',
    2, 2, '1.0.0', 1, 3, 3, NOW(),
    '{
       "version": "1.0.0",
       "activeBodyPoints": ["right_forearm"],
       "bodyPointZones": [
         {"id":"zone_right_forearm","bodyPoint":"right_forearm","section":"right_arm","x":0.66,"y":0.10,"w":0.34,"h":0.45}
       ],
       "shapes": [
         {"id":"start_1","type":"circle","role":"start","x":0.70,"y":0.45,"r":0.025},
         {"id":"finish_1","type":"circle","role":"finish","x":0.96,"y":0.15,"r":0.025}
       ],
       "tracks": [
         {"id":"track_1","type":"template","template":"arc","zone":"zone_right_forearm",
          "points":[[0.70,0.45],[0.78,0.30],[0.86,0.18],[0.96,0.15]],
          "corridorWidthMm":80}
       ],
       "corridors": [
         {"id":"corridor_1","trackId":"track_1","widthMm":80,"allowedExits":3,"timeOutsideLimitSec":2}
       ],
       "pacers": [
         {"id":"pacer_1","trackId":"track_1","speedMmSec":120,"shape":"circle","color":"#FFA500","loop":false}
       ],
       "cursors": [
         {"id":"cursor_1","bodyPoint":"right_forearm","trackerId":"VIVE-001","color":"#3366FF","size":24}
       ],
       "rules": [
         {"id":"r_finish","when":"cursor_inside_shape","shapeId":"finish_1","holdSec":1.5,"action":"complete"}
       ],
       "metricsConfig": ["meanDistanceToTrackCenterMm","timeOutsideCorridorPct","corridorExitCount","pacerLagPct","pacerLeadPct","durationSec"],
       "runtimeConfig": {"timeLimitSec":120,"countdownSec":3}
     }'::jsonb,
    'demo_hash_dev_only'
);


-- ----------------------------------------------------------------------------
-- 7. Демо-назначение упражнения пациенту
-- ----------------------------------------------------------------------------
INSERT INTO exercises (name, template_id, patient_id, assigned_by_id, status, scheduled_at) VALUES
    ('Дуга правой рукой — Пётр Тестовый', 1, 1, 2, 3, NOW() + INTERVAL '1 day');


COMMIT;

-- ----------------------------------------------------------------------------
-- Контроль
-- ----------------------------------------------------------------------------
SELECT 'body_points'      tbl, count(*) n FROM body_points
UNION ALL SELECT 'organizations',         count(*) FROM organizations
UNION ALL SELECT 'users',                 count(*) FROM users
UNION ALL SELECT 'patients',              count(*) FROM patients
UNION ALL SELECT 'devices',               count(*) FROM devices
UNION ALL SELECT 'calibration_profiles',  count(*) FROM calibration_profiles
UNION ALL SELECT 'trackers',              count(*) FROM trackers
UNION ALL SELECT 'exercise_templates',    count(*) FROM exercise_templates
UNION ALL SELECT 'exercises',             count(*) FROM exercises
ORDER BY tbl;
