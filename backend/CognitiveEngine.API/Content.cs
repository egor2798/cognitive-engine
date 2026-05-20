using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CognitiveEngine.API.Models.Enums;

namespace CognitiveEngine.API.Models;

// ============================================================================
//  Контентный слой: шаблоны упражнений, конкретные назначения, free-line.
//  Док.03, Док.04 §15-17, Док.05 §20.1.
// ============================================================================

/// <summary>
/// Шаблон упражнения — переиспользуемая конфигурация.
/// 3 уровня (Док.04 §16.1): system / organization / personal.
/// Полный конфиг (зоны, треки, фигуры, коридоры, пейсмекеры, курсоры, правила, метрики)
/// хранится в ConfigJson — реляционная декомпозиция тут только запутывает.
/// </summary>
public class ExerciseTemplate
{
    public int Id { get; set; }

    [Required, MaxLength(256)] public string Name { get; set; } = string.Empty;
    [MaxLength(2000)] public string? Description { get; set; }

    public TemplateScope Scope { get; set; } = TemplateScope.Personal;
    public TemplateStatus Status { get; set; } = TemplateStatus.Draft;

    /// <summary>SemVer-like версия. Любое изменение опубликованного шаблона = новая версия.</summary>
    [Required, MaxLength(32)] public string Version { get; set; } = "1.0.0";

    /// <summary>Если не первая версия — ссылка на предыдущую.</summary>
    public int? PreviousVersionId { get; set; }
    public ExerciseTemplate? PreviousVersion { get; set; }

    public int? OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    /// <summary>Для персональных шаблонов.</summary>
    public int? PatientId { get; set; }
    public Patient? Patient { get; set; }

    public int? CreatedById { get; set; }
    public User? CreatedBy { get; set; }

    public int? PublishedById { get; set; }
    public User? PublishedBy { get; set; }
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// Полный configJson (Док.06 §26.4):
    ///   activeBodyPoints[], bodyPointZones[], shapes[], tracks[], corridors[],
    ///   pacers[], cursors[], rules[], stimuli[], metricsConfig, runtimeConfig, reportConfig.
    /// Ключи объектов внутри (zoneId, trackId, shapeId, ...) стабильны и используются
    /// в SessionSamples/SessionEvents для привязки телеметрии.
    /// </summary>
    [Required, Column(TypeName = "jsonb")] public string ConfigJson { get; set; } = "{}";

    [MaxLength(64)] public string? ConfigHash { get; set; }    // sha256 от ConfigJson — для validate/idempotency

    [MaxLength(512)] public string? PreviewImageUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<Exercise> Exercises { get; set; } = new();
}


/// <summary>
/// Конкретное назначение упражнения для пациента (Док.06 §26.2 «Exercise»).
/// Хранит ссылку на шаблон + точечные адаптации под пациента (масштаб зон, ширина коридора и т.п.).
/// При запуске сессии формируется configSnapshot — иммутабельная копия в Session.
/// </summary>
public class Exercise
{
    public int Id { get; set; }

    [Required, MaxLength(256)] public string Name { get; set; } = string.Empty;

    public int TemplateId { get; set; }
    public ExerciseTemplate? Template { get; set; }

    public int PatientId { get; set; }
    public Patient? Patient { get; set; }

    public int? AssignedById { get; set; }
    public User? AssignedBy { get; set; }

    /// <summary>Точечные изменения относительно ConfigJson шаблона (override).</summary>
    [Column(TypeName = "jsonb")] public string? AdaptationJson { get; set; }

    public ExerciseStatus Status { get; set; } = ExerciseStatus.Draft;

    /// <summary>Запланированная дата (опц., календарное назначение).</summary>
    public DateTime? ScheduledAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<Session> Sessions { get; set; } = new();
}


/// <summary>
/// Запись фактического движения (Док.04 §15). Превращается в DerivedTrack.
/// </summary>
public class FreeTrace
{
    public int Id { get; set; }

    public int PatientId { get; set; }
    public Patient? Patient { get; set; }

    /// <summary>Сессия, в рамках которой шла запись. null если запись вне сессии (диагностический режим).</summary>
    public int? SessionId { get; set; }
    public Session? Session { get; set; }

    public int BodyPointId { get; set; }
    public BodyPoint? BodyPoint { get; set; }

    /// <summary>Стабильный ID зоны из configSnapshot сессии (если был).</summary>
    [MaxLength(64)] public string? BodyPointZoneRef { get; set; }

    public int? CalibrationProfileId { get; set; }
    public CalibrationProfile? CalibrationProfile { get; set; }

    public int? TrackerId { get; set; }
    public Tracker? Tracker { get; set; }

    /// <summary>Сырые точки до фильтрации.</summary>
    [Column(TypeName = "jsonb")] public string RawSamples { get; set; } = "[]";

    /// <summary>Точки после фильтрации/сглаживания.</summary>
    [Column(TypeName = "jsonb")] public string? FilteredSamples { get; set; }

    /// <summary>Выбранный врачом полезный участок (start/end индексы, флаги).</summary>
    [Column(TypeName = "jsonb")] public string? SelectedSegment { get; set; }

    /// <summary>Параметры очистки/сглаживания/выбросов.</summary>
    [Column(TypeName = "jsonb")] public string? FilterParams { get; set; }

    public FreeTraceStatus Status { get; set; } = FreeTraceStatus.Recording;
    [MaxLength(32)] public string Source { get; set; } = "realTracker";   // realTracker / emulator / imported

    public int? CreatedById { get; set; }
    public User? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}


/// <summary>
/// Трек, сгенерированный из FreeTrace (Док.04 §15.3).
/// </summary>
public class DerivedTrack
{
    public int Id { get; set; }

    [Required, MaxLength(256)] public string Name { get; set; } = string.Empty;

    public int PatientId { get; set; }
    public Patient? Patient { get; set; }

    public int SourceFreeTraceId { get; set; }
    public FreeTrace? SourceFreeTrace { get; set; }

    /// <summary>Точки трека в нормализованных координатах рабочей плоскости.</summary>
    [Required, Column(TypeName = "jsonb")] public string TrackPoints { get; set; } = "[]";

    /// <summary>Сегменты (прямые/кривые/безье).</summary>
    [Column(TypeName = "jsonb")] public string? Segments { get; set; }

    [Column(TypeName = "numeric(10,2)")] public decimal? LengthMm { get; set; }

    [MaxLength(32)] public string Direction { get; set; } = "forward";    // forward / backward / bidirectional

    [Column(TypeName = "jsonb")] public string? CorridorDefaults { get; set; }
    [Column(TypeName = "jsonb")] public string? PacerDefaults { get; set; }

    [Required, MaxLength(32)] public string Version { get; set; } = "1.0.0";

    /// <summary>Если трек сохранён как часть персонального шаблона пациента.</summary>
    public int? PatientTemplateId { get; set; }
    public ExerciseTemplate? PatientTemplate { get; set; }

    public int? CreatedById { get; set; }
    public User? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
