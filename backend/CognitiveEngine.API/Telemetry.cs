using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CognitiveEngine.API.Models.Enums;

namespace CognitiveEngine.API.Models;

// ============================================================================
//  Сессионный/телеметрический слой. Док.05 §20-22, Док.06 §26.3.
//
//  Иммутабельные snapshot'ы конфигурации и калибровки в Session делают отчёт
//  воспроизводимым даже после изменения шаблона / профиля.
// ============================================================================

/// <summary>
/// Прохождение упражнения. Хранит snapshot конфигурации и калибровки на момент запуска.
/// </summary>
public class Session
{
    public int Id { get; set; }

    public int ExerciseId { get; set; }
    public Exercise? Exercise { get; set; }

    public int PatientId { get; set; }
    public Patient? Patient { get; set; }

    public int? OperatorId { get; set; }
    public User? Operator { get; set; }

    public int DeviceId { get; set; }
    public Device? Device { get; set; }

    public int CalibrationProfileId { get; set; }
    public CalibrationProfile? CalibrationProfile { get; set; }

    /// <summary>Версия шаблона, по которой запускалась сессия (для отчётности и отладки).</summary>
    [MaxLength(32)] public string? TemplateVersion { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedAt { get; set; }

    public SessionStatus Status { get; set; } = SessionStatus.ConfigLoaded;
    public string? AbortReason { get; set; }

    /// <summary>Иммутабельный снимок конфига упражнения на момент старта (Док.06 §26.3, §26.4).</summary>
    [Required, Column(TypeName = "jsonb")] public string ExerciseConfigSnapshot { get; set; } = "{}";

    /// <summary>Иммутабельный снимок параметров калибровки (масштаб, углы, секции) на момент старта.</summary>
    [Required, Column(TypeName = "jsonb")] public string CalibrationSnapshot { get; set; } = "{}";

    /// <summary>SHA-256 от ExerciseConfigSnapshot — служит ключом валидации Player ↔ Backend.</summary>
    [MaxLength(64)] public string? ConfigHash { get; set; }

    /// <summary>true если сессия была собрана из локального буфера в offline-режиме (Док.05 §20.5).</summary>
    public bool WasOffline { get; set; }

    /// <summary>Заключение врача / комментарий по сессии.</summary>
    public string? OperatorNote { get; set; }

    /// <summary>Краткий итог от ИИ-ассистента, если используется.</summary>
    public string? AiSummary { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<SessionEvent> Events { get; set; } = new();
    public List<SessionSample> Samples { get; set; } = new();
    public List<Metric> Metrics { get; set; } = new();
    public List<Report> Reports { get; set; } = new();
    public List<Comment> Comments { get; set; } = new();
}


/// <summary>
/// Точка временного ряда: координаты курсора или пейсмекера.
/// HOT-таблица, миллионы строк. Разделение по SampleKind вместо двух таблиц —
/// упрощает batch-загрузку и общие индексы.
///
/// Идемпотентность batch-вставки достигается уникальностью (session_id, kind, sample_index)
/// и заголовком idempotency_key на эндпоинте.
/// </summary>
public class SessionSample
{
    public long Id { get; set; }   // BIGSERIAL

    public int SessionId { get; set; }
    public Session? Session { get; set; }

    public SampleKind Kind { get; set; }            // 1=Cursor, 2=Pacer

    /// <summary>Порядковый номер в потоке kind в рамках сессии. Монотонный, без пропусков.</summary>
    public long SampleIndex { get; set; }

    /// <summary>Время от старта сессии, сек.</summary>
    [Column(TypeName = "numeric(10,4)")] public decimal T { get; set; }

    /// <summary>Cursor 1/2/3 (для Kind=Cursor) или Pacer 1..N.</summary>
    public int SlotIndex { get; set; }

    /// <summary>Стабильные строковые id из ExerciseConfigSnapshot (cursor_1, pacer_1, track_1, zone_right_arm и т.п.).</summary>
    [MaxLength(64)] public string? CursorRef { get; set; }
    [MaxLength(64)] public string? PacerRef { get; set; }
    [MaxLength(64)] public string? TrackRef { get; set; }
    [MaxLength(64)] public string? BodyPointZoneRef { get; set; }
    [MaxLength(64)] public string? NearestShapeRef { get; set; }

    /// <summary>FK на справочник 11 точек тела (только для Kind=Cursor).</summary>
    public int? BodyPointId { get; set; }
    public BodyPoint? BodyPoint { get; set; }

    public int? TrackerId { get; set; }
    public Tracker? Tracker { get; set; }

    // ----- координаты -----
    [Column(TypeName = "numeric(10,4)")] public decimal? RawX { get; set; }
    [Column(TypeName = "numeric(10,4)")] public decimal? RawY { get; set; }
    [Column(TypeName = "numeric(10,4)")] public decimal? RawZ { get; set; }

    [Column(TypeName = "numeric(10,4)")] public decimal? CorrectedX { get; set; }
    [Column(TypeName = "numeric(10,4)")] public decimal? CorrectedY { get; set; }
    [Column(TypeName = "numeric(10,4)")] public decimal? CorrectedZ { get; set; }

    /// <summary>Координаты на рабочей плоскости (нормализованные 0..1).</summary>
    [Column(TypeName = "numeric(8,6)")] public decimal? ScreenX { get; set; }
    [Column(TypeName = "numeric(8,6)")] public decimal? ScreenY { get; set; }

    /// <summary>Физические координаты, мм (после калибровки).</summary>
    [Column(TypeName = "numeric(10,2)")] public decimal? Xmm { get; set; }
    [Column(TypeName = "numeric(10,2)")] public decimal? Ymm { get; set; }

    [Column(TypeName = "numeric(10,2)")] public decimal? Speed { get; set; }
    [Column(TypeName = "numeric(10,2)")] public decimal? Acceleration { get; set; }

    public bool? InsideZone { get; set; }
    public bool? InsideCorridor { get; set; }

    [Column(TypeName = "numeric(10,2)")] public decimal? DistanceToTrackCenterMm { get; set; }
    [Column(TypeName = "numeric(10,2)")] public decimal? DistanceToCorridorBorderMm { get; set; }
    [Column(TypeName = "numeric(10,2)")] public decimal? DistanceToPacerMm { get; set; }

    public SignalStatus SignalStatus { get; set; } = SignalStatus.Ok;
    [Column(TypeName = "numeric(4,3)")] public decimal? Confidence { get; set; }

    /// <summary>Все остальные поля, что не вошли в горячие колонки (квартернионы, debug-данные и т.д.).</summary>
    [Column(TypeName = "jsonb")] public string? ExtraJson { get; set; }
}


/// <summary>
/// Иммутабельное событие в журнале сессии (Док.05 §21.3).
/// Не редактируется: коррекция = новое событие.
/// </summary>
public class SessionEvent
{
    public long Id { get; set; }

    public int SessionId { get; set; }
    public Session? Session { get; set; }

    public SessionEventType Type { get; set; }

    /// <summary>Время от старта сессии, сек.</summary>
    [Column(TypeName = "numeric(10,4)")] public decimal T { get; set; }

    [MaxLength(64)] public string? CursorRef { get; set; }
    [MaxLength(64)] public string? PacerRef { get; set; }
    [MaxLength(64)] public string? TrackRef { get; set; }
    [MaxLength(64)] public string? BodyPointZoneRef { get; set; }
    [MaxLength(64)] public string? ShapeRef { get; set; }

    public int? TrackerId { get; set; }
    public Tracker? Tracker { get; set; }

    public int? OperatorId { get; set; }
    public User? Operator { get; set; }

    /// <summary>Полная нагрузка события: distanceMm, deltaTimeMs, holdTime, oldOffset/newOffset и т.п.</summary>
    [Column(TypeName = "jsonb")] public string Payload { get; set; } = "{}";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}


/// <summary>
/// Расчётный показатель (Док.05 §22). Хранится по уровням агрегации:
/// session/exercise/zone/bodyPoint/track/segment/shape/corridor/pacer/dynamics.
/// </summary>
public class Metric
{
    public int Id { get; set; }

    public int SessionId { get; set; }
    public Session? Session { get; set; }

    public MetricScope Scope { get; set; }

    /// <summary>ID объекта внутри scope: zoneRef, trackRef, shapeRef и т.д. null для scope=Session.</summary>
    [MaxLength(64)] public string? ScopeRef { get; set; }

    /// <summary>Машинное имя метрики: meanDistanceToTrackCenterMm, timeOutsideCorridorPct, и т.п.</summary>
    [Required, MaxLength(64)] public string Code { get; set; } = string.Empty;

    [Column(TypeName = "numeric(14,4)")] public decimal Value { get; set; }
    [MaxLength(16)] public string Unit { get; set; } = string.Empty;

    [Column(TypeName = "numeric(14,4)")] public decimal? Threshold { get; set; }
    [MaxLength(16)] public string? ThresholdOperator { get; set; }     // <= / >= / between

    public MetricQuality Quality { get; set; } = MetricQuality.Valid;

    /// <summary>Версия алгоритма расчёта — для воспроизводимости при пересчёте.</summary>
    [Required, MaxLength(32)] public string AlgorithmVersion { get; set; } = "1.0.0";

    /// <summary>Доп. данные (детализация, интервалы доверия и т.д.).</summary>
    [Column(TypeName = "jsonb")] public string? DetailsJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
