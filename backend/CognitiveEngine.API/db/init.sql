-- ============================================================================
-- Cognitive Engine — схема БД v2 (PostgreSQL 14+)
-- Соответствует Документу 07 (разделы 1-31, версия v1).
--
-- Применить:
--   psql -U cogengine -d cognitive_engine -f db/init.sql
--
-- Эта схема — точное соответствие EF Core моделям из backend/CognitiveEngine.API/Models.
-- ============================================================================

BEGIN;

-- ----------------------------------------------------------------------------
-- 1. Расширения
-- ----------------------------------------------------------------------------
CREATE EXTENSION IF NOT EXISTS "pgcrypto";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

-- ============================================================================
-- АДМИНИСТРИРОВАНИЕ / ИДЕНТИЧНОСТЬ
-- ============================================================================

CREATE TABLE organizations (
    id           SERIAL PRIMARY KEY,
    name         VARCHAR(256) NOT NULL,
    type         INT NOT NULL DEFAULT 5,
    inn          VARCHAR(12),
    address      VARCHAR(512),
    phone        VARCHAR(32),
    email        VARCHAR(256),
    is_active    BOOLEAN NOT NULL DEFAULT TRUE,
    created_at   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT ck_org_type CHECK (type BETWEEN 1 AND 5)
);
CREATE UNIQUE INDEX ux_organizations_inn ON organizations(inn) WHERE inn IS NOT NULL;


CREATE TABLE users (
    id                       SERIAL PRIMARY KEY,
    login                    VARCHAR(32) NOT NULL,
    email                    VARCHAR(256) NOT NULL,
    first_name               VARCHAR(128),
    last_name                VARCHAR(128),
    middle_name              VARCHAR(128),
    phone                    VARCHAR(32),
    password_hash            VARCHAR(512) NOT NULL,
    role                     INT NOT NULL DEFAULT 1,
    organization_id          INT NOT NULL,
    is_active                BOOLEAN NOT NULL DEFAULT TRUE,
    failed_login_attempts    INT NOT NULL DEFAULT 0,
    locked_until             TIMESTAMPTZ,
    last_login_at            TIMESTAMPTZ,
    avatar_url               VARCHAR(512),
    created_at               TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at               TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_users_org           FOREIGN KEY (organization_id) REFERENCES organizations(id) ON DELETE RESTRICT,
    CONSTRAINT ck_users_role          CHECK (role BETWEEN 1 AND 5),
    CONSTRAINT ck_users_login_format  CHECK (login ~ '^[A-Za-z0-9_.]{3,32}$')
);
CREATE UNIQUE INDEX ux_users_login    ON users(LOWER(login));
CREATE UNIQUE INDEX ux_users_email    ON users(LOWER(email));
CREATE        INDEX ix_users_org_role ON users(organization_id, role);


CREATE TABLE patients (
    id                  SERIAL PRIMARY KEY,
    code                VARCHAR(64) NOT NULL,
    first_name          VARCHAR(128),
    last_name           VARCHAR(128),
    middle_name         VARCHAR(128),
    birth_date          DATE,
    gender              INT,
    height_cm           NUMERIC(5,2),
    weight_kg           NUMERIC(5,2),
    restrictions        TEXT,
    email               VARCHAR(256),
    phone               VARCHAR(32),
    avatar_url          VARCHAR(512),
    organization_id     INT NOT NULL,
    user_id             INT,
    is_depersonalized   BOOLEAN NOT NULL DEFAULT FALSE,
    profile_data        JSONB NOT NULL DEFAULT '{}'::jsonb,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_patients_org   FOREIGN KEY (organization_id) REFERENCES organizations(id) ON DELETE RESTRICT,
    CONSTRAINT fk_patients_user  FOREIGN KEY (user_id)         REFERENCES users(id)         ON DELETE SET NULL,
    CONSTRAINT ux_patients_user  UNIQUE (user_id),
    CONSTRAINT ck_patients_gender CHECK (gender IS NULL OR gender BETWEEN 1 AND 3)
);
CREATE UNIQUE INDEX ux_patients_org_code ON patients(organization_id, code);
CREATE        INDEX ix_patients_org      ON patients(organization_id);
CREATE        INDEX ix_patients_fullname ON patients(last_name, first_name);
CREATE        INDEX ix_patients_birth    ON patients(birth_date);
CREATE        INDEX ix_patients_profile  ON patients USING GIN (profile_data);
CREATE        INDEX ix_patients_lastname_trgm ON patients USING GIN (last_name gin_trgm_ops);


CREATE TABLE consents (
    id                  SERIAL PRIMARY KEY,
    patient_id          INT NOT NULL,
    type                INT NOT NULL,
    granted             BOOLEAN NOT NULL,
    agreement_version   VARCHAR(32),
    ip_address          VARCHAR(64),
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_consents_patient FOREIGN KEY (patient_id) REFERENCES patients(id) ON DELETE CASCADE,
    CONSTRAINT ck_consents_type    CHECK (type BETWEEN 1 AND 4)
);
CREATE INDEX ix_consents_lookup ON consents(patient_id, type, created_at DESC);


-- ============================================================================
-- АППАРАТНЫЙ СЛОЙ
-- ============================================================================

CREATE TABLE body_points (
    id              SERIAL PRIMARY KEY,
    code            VARCHAR(32) NOT NULL,
    name            VARCHAR(128) NOT NULL,
    side            INT NOT NULL DEFAULT 1,
    "group"         INT NOT NULL DEFAULT 2,
    order_index     INT NOT NULL,
    CONSTRAINT ck_bp_side  CHECK (side BETWEEN 1 AND 3),
    CONSTRAINT ck_bp_group CHECK ("group" BETWEEN 1 AND 7)
);
CREATE UNIQUE INDEX ux_body_points_code  ON body_points(code);
CREATE UNIQUE INDEX ux_body_points_order ON body_points(order_index);


CREATE TABLE devices (
    id                              SERIAL PRIMARY KEY,
    name                            VARCHAR(128) NOT NULL,
    type                            INT NOT NULL DEFAULT 1,
    location                        VARCHAR(256),
    serial_number                   VARCHAR(128),
    width_px                        INT NOT NULL,
    height_px                       INT NOT NULL,
    is_portrait                     BOOLEAN NOT NULL DEFAULT TRUE,
    physical_width_mm               NUMERIC(8,2),
    physical_height_mm              NUMERIC(8,2),
    status                          INT NOT NULL DEFAULT 1,
    organization_id                 INT NOT NULL,
    active_calibration_profile_id   INT,
    created_at                      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at                      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_devices_org   FOREIGN KEY (organization_id) REFERENCES organizations(id) ON DELETE RESTRICT,
    CONSTRAINT ck_devices_type  CHECK (type   BETWEEN 1 AND 4),
    CONSTRAINT ck_devices_stat  CHECK (status BETWEEN 1 AND 4)
);
CREATE UNIQUE INDEX ux_devices_serial ON devices(serial_number) WHERE serial_number IS NOT NULL;
CREATE        INDEX ix_devices_org    ON devices(organization_id, status);


CREATE TABLE calibration_profiles (
    id                      SERIAL PRIMARY KEY,
    name                    VARCHAR(128) NOT NULL,
    device_id               INT NOT NULL,
    mode                    INT NOT NULL DEFAULT 1,
    patient_id              INT,
    width_px                INT NOT NULL,
    height_px               INT NOT NULL,
    physical_width_mm       NUMERIC(8,2),
    physical_height_mm      NUMERIC(8,2),
    scale_x                 NUMERIC(10,5),
    scale_y                 NUMERIC(10,5),
    offset_x_mm             NUMERIC(8,2),
    offset_y_mm             NUMERIC(8,2),
    patient_height_cm       NUMERIC(5,2),
    config_json             JSONB NOT NULL DEFAULT '{}'::jsonb,
    is_active               BOOLEAN NOT NULL DEFAULT TRUE,
    valid_until             TIMESTAMPTZ,
    created_by_id           INT,
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_cal_device  FOREIGN KEY (device_id)     REFERENCES devices(id)  ON DELETE CASCADE,
    CONSTRAINT fk_cal_patient FOREIGN KEY (patient_id)    REFERENCES patients(id) ON DELETE SET NULL,
    CONSTRAINT fk_cal_creator FOREIGN KEY (created_by_id) REFERENCES users(id)    ON DELETE SET NULL,
    CONSTRAINT ck_cal_mode    CHECK (mode BETWEEN 1 AND 3)
);
CREATE INDEX ix_cal_device_active ON calibration_profiles(device_id, is_active);
CREATE INDEX ix_cal_patient       ON calibration_profiles(patient_id);

-- замыкаем FK devices.active_calibration_profile_id, теперь когда таблица calibration_profiles существует
ALTER TABLE devices
    ADD CONSTRAINT fk_devices_active_cal
        FOREIGN KEY (active_calibration_profile_id)
        REFERENCES calibration_profiles(id) ON DELETE SET NULL;


CREATE TABLE trackers (
    id              SERIAL PRIMARY KEY,
    name            VARCHAR(128) NOT NULL,
    type            INT NOT NULL DEFAULT 2,
    serial_number   VARCHAR(128),
    device_id       INT NOT NULL,
    body_point_id   INT,
    status          INT NOT NULL DEFAULT 1,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_tr_device FOREIGN KEY (device_id)     REFERENCES devices(id)     ON DELETE CASCADE,
    CONSTRAINT fk_tr_bp     FOREIGN KEY (body_point_id) REFERENCES body_points(id) ON DELETE SET NULL,
    CONSTRAINT ck_tr_type   CHECK (type   BETWEEN 1 AND 4),
    CONSTRAINT ck_tr_status CHECK (status BETWEEN 1 AND 5)
);
CREATE UNIQUE INDEX ux_trackers_serial ON trackers(serial_number) WHERE serial_number IS NOT NULL;
CREATE        INDEX ix_trackers_device ON trackers(device_id, status);


-- ============================================================================
-- КОНТЕНТНЫЙ СЛОЙ
-- ============================================================================

CREATE TABLE exercise_templates (
    id                      SERIAL PRIMARY KEY,
    name                    VARCHAR(256) NOT NULL,
    description             VARCHAR(2000),
    scope                   INT NOT NULL DEFAULT 3,
    status                  INT NOT NULL DEFAULT 1,
    version                 VARCHAR(32) NOT NULL DEFAULT '1.0.0',
    previous_version_id     INT,
    organization_id         INT,
    patient_id              INT,
    created_by_id           INT,
    published_by_id         INT,
    published_at            TIMESTAMPTZ,
    config_json             JSONB NOT NULL DEFAULT '{}'::jsonb,
    config_hash             VARCHAR(64),
    preview_image_url       VARCHAR(512),
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_tpl_prev      FOREIGN KEY (previous_version_id) REFERENCES exercise_templates(id) ON DELETE SET NULL,
    CONSTRAINT fk_tpl_org       FOREIGN KEY (organization_id)     REFERENCES organizations(id)      ON DELETE SET NULL,
    CONSTRAINT fk_tpl_patient   FOREIGN KEY (patient_id)          REFERENCES patients(id)           ON DELETE SET NULL,
    CONSTRAINT fk_tpl_creator   FOREIGN KEY (created_by_id)       REFERENCES users(id)              ON DELETE SET NULL,
    CONSTRAINT fk_tpl_publisher FOREIGN KEY (published_by_id)     REFERENCES users(id)              ON DELETE SET NULL,
    CONSTRAINT ck_tpl_scope     CHECK (scope  BETWEEN 1 AND 3),
    CONSTRAINT ck_tpl_status    CHECK (status BETWEEN 1 AND 4)
);
CREATE INDEX ix_tpl_scope_status ON exercise_templates(scope, status);
CREATE INDEX ix_tpl_org          ON exercise_templates(organization_id);
CREATE INDEX ix_tpl_patient      ON exercise_templates(patient_id);
CREATE INDEX ix_tpl_hash         ON exercise_templates(config_hash);
CREATE INDEX ix_tpl_config_gin   ON exercise_templates USING GIN (config_json);


CREATE TABLE exercises (
    id                  SERIAL PRIMARY KEY,
    name                VARCHAR(256) NOT NULL,
    template_id         INT NOT NULL,
    patient_id          INT NOT NULL,
    assigned_by_id      INT,
    adaptation_json     JSONB,
    status              INT NOT NULL DEFAULT 1,
    scheduled_at        TIMESTAMPTZ,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_ex_tpl       FOREIGN KEY (template_id)    REFERENCES exercise_templates(id) ON DELETE RESTRICT,
    CONSTRAINT fk_ex_patient   FOREIGN KEY (patient_id)     REFERENCES patients(id)           ON DELETE CASCADE,
    CONSTRAINT fk_ex_assigner  FOREIGN KEY (assigned_by_id) REFERENCES users(id)              ON DELETE SET NULL,
    CONSTRAINT ck_ex_status    CHECK (status BETWEEN 1 AND 7)
);
CREATE INDEX ix_ex_patient_status ON exercises(patient_id, status);
CREATE INDEX ix_ex_template       ON exercises(template_id);
CREATE INDEX ix_ex_scheduled      ON exercises(scheduled_at) WHERE scheduled_at IS NOT NULL;


-- ============================================================================
-- СЕССИИ И ТЕЛЕМЕТРИЯ
-- ============================================================================

CREATE TABLE sessions (
    id                          SERIAL PRIMARY KEY,
    exercise_id                 INT NOT NULL,
    patient_id                  INT NOT NULL,
    operator_id                 INT,
    device_id                   INT NOT NULL,
    calibration_profile_id      INT NOT NULL,
    template_version            VARCHAR(32),
    started_at                  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    finished_at                 TIMESTAMPTZ,
    status                      INT NOT NULL DEFAULT 1,
    abort_reason                TEXT,
    exercise_config_snapshot    JSONB NOT NULL DEFAULT '{}'::jsonb,
    calibration_snapshot        JSONB NOT NULL DEFAULT '{}'::jsonb,
    config_hash                 VARCHAR(64),
    was_offline                 BOOLEAN NOT NULL DEFAULT FALSE,
    operator_note               TEXT,
    ai_summary                  TEXT,
    created_at                  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at                  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_se_exercise FOREIGN KEY (exercise_id)            REFERENCES exercises(id)             ON DELETE RESTRICT,
    CONSTRAINT fk_se_patient  FOREIGN KEY (patient_id)             REFERENCES patients(id)              ON DELETE CASCADE,
    CONSTRAINT fk_se_operator FOREIGN KEY (operator_id)            REFERENCES users(id)                 ON DELETE SET NULL,
    CONSTRAINT fk_se_device   FOREIGN KEY (device_id)              REFERENCES devices(id)               ON DELETE RESTRICT,
    CONSTRAINT fk_se_cal      FOREIGN KEY (calibration_profile_id) REFERENCES calibration_profiles(id)  ON DELETE RESTRICT,
    CONSTRAINT ck_se_status   CHECK (status BETWEEN 1 AND 10),
    CONSTRAINT ck_se_finish   CHECK (finished_at IS NULL OR finished_at >= started_at)
);
CREATE INDEX ix_se_patient_started  ON sessions(patient_id, started_at DESC);
CREATE INDEX ix_se_exercise_started ON sessions(exercise_id, started_at DESC);
CREATE INDEX ix_se_device           ON sessions(device_id);
CREATE INDEX ix_se_started          ON sessions(started_at);
CREATE INDEX ix_se_hash             ON sessions(config_hash);


-- session_samples — ГОРЯЧАЯ таблица. Большой объём.
-- BIGSERIAL id, композитный уникум (session_id, kind, sample_index) для идемпотентности.
CREATE TABLE session_samples (
    id                                  BIGSERIAL PRIMARY KEY,
    session_id                          INT NOT NULL,
    kind                                INT NOT NULL,
    sample_index                        BIGINT NOT NULL,
    t                                   NUMERIC(10,4) NOT NULL,
    slot_index                          INT NOT NULL,
    cursor_ref                          VARCHAR(64),
    pacer_ref                           VARCHAR(64),
    track_ref                           VARCHAR(64),
    body_point_zone_ref                 VARCHAR(64),
    nearest_shape_ref                   VARCHAR(64),
    body_point_id                       INT,
    tracker_id                          INT,
    raw_x                               NUMERIC(10,4),
    raw_y                               NUMERIC(10,4),
    raw_z                               NUMERIC(10,4),
    corrected_x                         NUMERIC(10,4),
    corrected_y                         NUMERIC(10,4),
    corrected_z                         NUMERIC(10,4),
    screen_x                            NUMERIC(8,6),
    screen_y                            NUMERIC(8,6),
    xmm                                 NUMERIC(10,2),
    ymm                                 NUMERIC(10,2),
    speed                               NUMERIC(10,2),
    acceleration                        NUMERIC(10,2),
    inside_zone                         BOOLEAN,
    inside_corridor                     BOOLEAN,
    distance_to_track_center_mm         NUMERIC(10,2),
    distance_to_corridor_border_mm      NUMERIC(10,2),
    distance_to_pacer_mm                NUMERIC(10,2),
    signal_status                       INT NOT NULL DEFAULT 1,
    confidence                          NUMERIC(4,3),
    extra_json                          JSONB,
    CONSTRAINT fk_ss_session  FOREIGN KEY (session_id)    REFERENCES sessions(id)    ON DELETE CASCADE,
    CONSTRAINT fk_ss_bp       FOREIGN KEY (body_point_id) REFERENCES body_points(id) ON DELETE SET NULL,
    CONSTRAINT fk_ss_tracker  FOREIGN KEY (tracker_id)    REFERENCES trackers(id)    ON DELETE SET NULL,
    CONSTRAINT ck_ss_kind     CHECK (kind BETWEEN 1 AND 2),
    CONSTRAINT ck_ss_signal   CHECK (signal_status BETWEEN 1 AND 4),
    CONSTRAINT ck_ss_conf     CHECK (confidence IS NULL OR confidence BETWEEN 0 AND 1)
);
-- идемпотентность batch-вставок
CREATE UNIQUE INDEX ux_ss_session_kind_idx ON session_samples(session_id, kind, sample_index);
-- горячий запрос: построение траектории
CREATE        INDEX ix_ss_session_t        ON session_samples(session_id, t);
CREATE        INDEX ix_ss_session_slot_t   ON session_samples(session_id, slot_index, t);
-- BRIN-индекс на t полезен при больших объёмах (монотонное время)
CREATE        INDEX ix_ss_t_brin           ON session_samples USING BRIN (t);


CREATE TABLE session_events (
    id                      BIGSERIAL PRIMARY KEY,
    session_id              INT NOT NULL,
    type                    INT NOT NULL,
    t                       NUMERIC(10,4) NOT NULL,
    cursor_ref              VARCHAR(64),
    pacer_ref               VARCHAR(64),
    track_ref               VARCHAR(64),
    body_point_zone_ref     VARCHAR(64),
    shape_ref               VARCHAR(64),
    tracker_id              INT,
    operator_id             INT,
    payload                 JSONB NOT NULL DEFAULT '{}'::jsonb,
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_ev_session  FOREIGN KEY (session_id)  REFERENCES sessions(id) ON DELETE CASCADE,
    CONSTRAINT fk_ev_tracker  FOREIGN KEY (tracker_id)  REFERENCES trackers(id) ON DELETE SET NULL,
    CONSTRAINT fk_ev_operator FOREIGN KEY (operator_id) REFERENCES users(id)    ON DELETE SET NULL
);
CREATE INDEX ix_ev_session_t    ON session_events(session_id, t);
CREATE INDEX ix_ev_session_type ON session_events(session_id, type);


CREATE TABLE metrics (
    id                  SERIAL PRIMARY KEY,
    session_id          INT NOT NULL,
    scope               INT NOT NULL,
    scope_ref           VARCHAR(64),
    code                VARCHAR(64) NOT NULL,
    value               NUMERIC(14,4) NOT NULL,
    unit                VARCHAR(16) NOT NULL DEFAULT '',
    threshold           NUMERIC(14,4),
    threshold_operator  VARCHAR(16),
    quality             INT NOT NULL DEFAULT 1,
    algorithm_version   VARCHAR(32) NOT NULL DEFAULT '1.0.0',
    details_json        JSONB,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_metr_session FOREIGN KEY (session_id) REFERENCES sessions(id) ON DELETE CASCADE,
    CONSTRAINT ck_metr_scope   CHECK (scope   BETWEEN 1 AND 10),
    CONSTRAINT ck_metr_quality CHECK (quality BETWEEN 1 AND 3)
);
CREATE INDEX ix_metr_session_scope_code ON metrics(session_id, scope, code);


-- ============================================================================
-- FREE-TRACE / DERIVED TRACK
-- ============================================================================

CREATE TABLE free_traces (
    id                      SERIAL PRIMARY KEY,
    patient_id              INT NOT NULL,
    session_id              INT,
    body_point_id           INT NOT NULL,
    body_point_zone_ref     VARCHAR(64),
    calibration_profile_id  INT,
    tracker_id              INT,
    raw_samples             JSONB NOT NULL DEFAULT '[]'::jsonb,
    filtered_samples        JSONB,
    selected_segment        JSONB,
    filter_params           JSONB,
    status                  INT NOT NULL DEFAULT 1,
    source                  VARCHAR(32) NOT NULL DEFAULT 'realTracker',
    created_by_id           INT,
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_ft_patient FOREIGN KEY (patient_id)             REFERENCES patients(id)             ON DELETE CASCADE,
    CONSTRAINT fk_ft_session FOREIGN KEY (session_id)             REFERENCES sessions(id)             ON DELETE SET NULL,
    CONSTRAINT fk_ft_bp      FOREIGN KEY (body_point_id)          REFERENCES body_points(id)          ON DELETE RESTRICT,
    CONSTRAINT fk_ft_cal     FOREIGN KEY (calibration_profile_id) REFERENCES calibration_profiles(id) ON DELETE SET NULL,
    CONSTRAINT fk_ft_tracker FOREIGN KEY (tracker_id)             REFERENCES trackers(id)             ON DELETE SET NULL,
    CONSTRAINT fk_ft_creator FOREIGN KEY (created_by_id)          REFERENCES users(id)                ON DELETE SET NULL,
    CONSTRAINT ck_ft_status  CHECK (status BETWEEN 1 AND 4)
);
CREATE INDEX ix_ft_patient ON free_traces(patient_id, created_at DESC);
CREATE INDEX ix_ft_session ON free_traces(session_id);


CREATE TABLE derived_tracks (
    id                      SERIAL PRIMARY KEY,
    name                    VARCHAR(256) NOT NULL,
    patient_id              INT NOT NULL,
    source_free_trace_id    INT NOT NULL,
    track_points            JSONB NOT NULL DEFAULT '[]'::jsonb,
    segments                JSONB,
    length_mm               NUMERIC(10,2),
    direction               VARCHAR(32) NOT NULL DEFAULT 'forward',
    corridor_defaults       JSONB,
    pacer_defaults          JSONB,
    version                 VARCHAR(32) NOT NULL DEFAULT '1.0.0',
    patient_template_id     INT,
    created_by_id           INT,
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_dt_patient FOREIGN KEY (patient_id)          REFERENCES patients(id)            ON DELETE CASCADE,
    CONSTRAINT fk_dt_source  FOREIGN KEY (source_free_trace_id) REFERENCES free_traces(id)         ON DELETE RESTRICT,
    CONSTRAINT fk_dt_tpl     FOREIGN KEY (patient_template_id) REFERENCES exercise_templates(id)  ON DELETE SET NULL,
    CONSTRAINT fk_dt_creator FOREIGN KEY (created_by_id)       REFERENCES users(id)               ON DELETE SET NULL
);
CREATE INDEX ix_dt_patient ON derived_tracks(patient_id);
CREATE INDEX ix_dt_source  ON derived_tracks(source_free_trace_id);


-- ============================================================================
-- ОТЧЁТЫ / ЭКСПОРТ / КОММУНИКАЦИЯ
-- ============================================================================

CREATE TABLE reports (
    id                          SERIAL PRIMARY KEY,
    session_id                  INT NOT NULL,
    type                        INT NOT NULL DEFAULT 1,
    config_json                 JSONB NOT NULL DEFAULT '{}'::jsonb,
    metrics_algorithm_version   VARCHAR(32),
    generated_by_id             INT,
    created_at                  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at                  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_rep_session FOREIGN KEY (session_id)      REFERENCES sessions(id) ON DELETE CASCADE,
    CONSTRAINT fk_rep_creator FOREIGN KEY (generated_by_id) REFERENCES users(id)    ON DELETE SET NULL,
    CONSTRAINT ck_rep_type    CHECK (type BETWEEN 1 AND 8)
);
CREATE INDEX ix_rep_session_type ON reports(session_id, type);


CREATE TABLE exports (
    id                  SERIAL PRIMARY KEY,
    owner_entity_type   VARCHAR(32) NOT NULL,
    owner_entity_id     INT NOT NULL,
    format              INT NOT NULL,
    status              INT NOT NULL DEFAULT 1,
    anonymized          BOOLEAN NOT NULL DEFAULT FALSE,
    params_json         JSONB,
    storage_key         VARCHAR(512),
    file_size_bytes     BIGINT,
    mime_type           VARCHAR(128),
    requested_by_id     INT,
    failure_reason      TEXT,
    report_id           INT,
    completed_at        TIMESTAMPTZ,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_exp_report  FOREIGN KEY (report_id)       REFERENCES reports(id) ON DELETE SET NULL,
    CONSTRAINT fk_exp_user    FOREIGN KEY (requested_by_id) REFERENCES users(id)   ON DELETE SET NULL,
    CONSTRAINT ck_exp_format  CHECK (format BETWEEN 1 AND 4),
    CONSTRAINT ck_exp_status  CHECK (status BETWEEN 1 AND 4)
);
CREATE INDEX ix_exp_owner  ON exports(owner_entity_type, owner_entity_id);
CREATE INDEX ix_exp_status ON exports(status);


CREATE TABLE comments (
    id                              SERIAL PRIMARY KEY,
    patient_id                      INT NOT NULL,
    session_id                      INT,
    author_id                       INT,
    text                            TEXT NOT NULL,
    visible_to_specialists_only     BOOLEAN NOT NULL DEFAULT FALSE,
    is_ai_generated                 BOOLEAN NOT NULL DEFAULT FALSE,
    created_at                      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at                      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_c_patient FOREIGN KEY (patient_id) REFERENCES patients(id)  ON DELETE CASCADE,
    CONSTRAINT fk_c_session FOREIGN KEY (session_id) REFERENCES sessions(id)  ON DELETE CASCADE,
    CONSTRAINT fk_c_author  FOREIGN KEY (author_id)  REFERENCES users(id)     ON DELETE SET NULL
);
CREATE INDEX ix_c_patient ON comments(patient_id, created_at DESC);
CREATE INDEX ix_c_session ON comments(session_id);


CREATE TABLE attachments (
    id                  SERIAL PRIMARY KEY,
    file_name           VARCHAR(256) NOT NULL,
    content_type        VARCHAR(128) NOT NULL,
    size_bytes          BIGINT NOT NULL,
    storage_key         VARCHAR(512) NOT NULL,
    owner_entity_type   VARCHAR(64) NOT NULL,
    owner_entity_id     INT NOT NULL,
    uploaded_by_id      INT,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_att_user FOREIGN KEY (uploaded_by_id) REFERENCES users(id) ON DELETE SET NULL
);
CREATE INDEX ix_att_owner ON attachments(owner_entity_type, owner_entity_id);


CREATE TABLE audit_logs (
    id                  BIGSERIAL PRIMARY KEY,
    category            INT NOT NULL,
    action              VARCHAR(64) NOT NULL,
    actor_user_id       INT,
    organization_id     INT,
    device_id           INT,
    session_id          INT,
    object_type         VARCHAR(64),
    object_id           INT,
    old_value           JSONB,
    new_value           JSONB,
    ip_address          VARCHAR(64),
    user_agent          VARCHAR(512),
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_au_user    FOREIGN KEY (actor_user_id)   REFERENCES users(id)         ON DELETE SET NULL,
    CONSTRAINT fk_au_org     FOREIGN KEY (organization_id) REFERENCES organizations(id) ON DELETE SET NULL,
    CONSTRAINT fk_au_device  FOREIGN KEY (device_id)       REFERENCES devices(id)       ON DELETE SET NULL,
    CONSTRAINT fk_au_session FOREIGN KEY (session_id)      REFERENCES sessions(id)      ON DELETE SET NULL,
    CONSTRAINT ck_au_cat     CHECK (category BETWEEN 1 AND 7)
);
CREATE INDEX ix_au_cat_time   ON audit_logs(category, created_at DESC);
CREATE INDEX ix_au_actor_time ON audit_logs(actor_user_id, created_at DESC);
CREATE INDEX ix_au_object     ON audit_logs(object_type, object_id);
CREATE INDEX ix_au_session    ON audit_logs(session_id);


-- ============================================================================
-- АУТЕНТИФИКАЦИЯ
-- ============================================================================

CREATE TABLE refresh_tokens (
    id              SERIAL PRIMARY KEY,
    user_id         INT NOT NULL,
    token_hash      VARCHAR(128) NOT NULL,
    expires_at      TIMESTAMPTZ NOT NULL,
    revoked_at      TIMESTAMPTZ,
    revoked_reason  TEXT,
    created_by_ip   VARCHAR(64),
    user_agent      VARCHAR(512),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_rt_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);
CREATE UNIQUE INDEX ux_rt_hash ON refresh_tokens(token_hash);
CREATE        INDEX ix_rt_user ON refresh_tokens(user_id, revoked_at);

CREATE TABLE password_reset_tokens (
    id                   SERIAL PRIMARY KEY,
    user_id              INT NOT NULL,
    token_hash           VARCHAR(128) NOT NULL,
    expires_at           TIMESTAMPTZ NOT NULL,
    used                 BOOLEAN NOT NULL DEFAULT FALSE,
    used_at              TIMESTAMPTZ,
    requested_from_ip    VARCHAR(64),
    created_at           TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_prt_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);
CREATE UNIQUE INDEX ux_prt_hash ON password_reset_tokens(token_hash);
CREATE        INDEX ix_prt_user ON password_reset_tokens(user_id, used);

CREATE TABLE login_attempts (
    id                BIGSERIAL PRIMARY KEY,
    login             VARCHAR(64) NOT NULL,
    user_id           INT,
    success           BOOLEAN NOT NULL,
    ip_address        VARCHAR(64),
    user_agent        VARCHAR(512),
    failure_reason    VARCHAR(128),
    created_at        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_la_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE SET NULL
);
CREATE INDEX ix_la_login ON login_attempts(login, created_at DESC);
CREATE INDEX ix_la_ip    ON login_attempts(ip_address, created_at DESC);


CREATE TABLE idempotency_keys (
    id                  BIGSERIAL PRIMARY KEY,
    key                 VARCHAR(128) NOT NULL,
    endpoint            VARCHAR(64) NOT NULL,
    session_id          INT,
    response_status     INT NOT NULL,
    response_body       JSONB,
    expires_at          TIMESTAMPTZ NOT NULL,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_idem_session FOREIGN KEY (session_id) REFERENCES sessions(id) ON DELETE CASCADE
);
CREATE UNIQUE INDEX ux_idem_endpoint_key ON idempotency_keys(endpoint, key);
CREATE        INDEX ix_idem_expires      ON idempotency_keys(expires_at);


-- ============================================================================
-- Триггер: автоматическое updated_at
-- ============================================================================
CREATE OR REPLACE FUNCTION touch_updated_at() RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DO $$
DECLARE
    t TEXT;
    tables TEXT[] := ARRAY[
        'organizations','users','patients','devices','calibration_profiles','trackers',
        'exercise_templates','exercises','sessions','free_traces','derived_tracks',
        'reports','comments'
    ];
BEGIN
    FOREACH t IN ARRAY tables LOOP
        EXECUTE format(
            'CREATE TRIGGER trg_%I_updated BEFORE UPDATE ON %I
             FOR EACH ROW EXECUTE FUNCTION touch_updated_at();', t, t);
    END LOOP;
END $$;

COMMIT;
