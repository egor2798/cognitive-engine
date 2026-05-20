using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CognitiveEngine.API.Models.Enums;

namespace CognitiveEngine.API.Models;

// ============================================================================
//  Отчёты, экспорт, комментарии, файлы, аудит, аутентификация.
//  Док.05 §23, Док.06 §25.4 / §27.
// ============================================================================

public class Report
{
    public int Id { get; set; }

    public int SessionId { get; set; }
    public Session? Session { get; set; }

    public ReportType Type { get; set; } = ReportType.Session;

    /// <summary>Конфигурация отображения отчёта (выбранные блоки, фильтры, настройки графиков).</summary>
    [Column(TypeName = "jsonb")] public string ConfigJson { get; set; } = "{}";

    /// <summary>Версия алгоритма метрик, под которую сформирован отчёт.</summary>
    [MaxLength(32)] public string? MetricsAlgorithmVersion { get; set; }

    public int? GeneratedById { get; set; }
    public User? GeneratedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<Export> Exports { get; set; } = new();
}


public class Export
{
    public int Id { get; set; }

    /// <summary>Может относиться к отчёту, сессии, пациенту или организации.</summary>
    [Required, MaxLength(32)] public string OwnerEntityType { get; set; } = string.Empty;
    public int OwnerEntityId { get; set; }

    public ExportFormat Format { get; set; }
    public ExportStatus Status { get; set; } = ExportStatus.Pending;

    /// <summary>true — выгрузка обезличена.</summary>
    public bool Anonymized { get; set; }

    /// <summary>Параметры экспорта: период, выбранные сессии/пациенты, фильтры по метрикам.</summary>
    [Column(TypeName = "jsonb")] public string? ParamsJson { get; set; }

    [MaxLength(512)] public string? StorageKey { get; set; }
    public long? FileSizeBytes { get; set; }
    [MaxLength(128)] public string? MimeType { get; set; }

    public int? RequestedById { get; set; }
    public User? RequestedBy { get; set; }

    public string? FailureReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    /// <summary>Опц. обратная связь с отчётом, если экспорт сделан из конкретного отчёта.</summary>
    public int? ReportId { get; set; }
    public Report? Report { get; set; }
}


/// <summary>
/// Комментарий специалиста в карточке пациента или по конкретной сессии (Док.05 §24.1, §23.3).
/// </summary>
public class Comment
{
    public int Id { get; set; }

    public int PatientId { get; set; }
    public Patient? Patient { get; set; }

    public int? SessionId { get; set; }
    public Session? Session { get; set; }

    /// <summary>Автор. null — для системных/AI-генерируемых заметок.</summary>
    public int? AuthorId { get; set; }
    public User? Author { get; set; }

    [Required] public string Text { get; set; } = string.Empty;

    /// <summary>Виден только сотрудникам организации.</summary>
    public bool VisibleToSpecialistsOnly { get; set; }

    public bool IsAiGenerated { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}


/// <summary>
/// Файлы: фото/видео-стимулы упражнений, аватары пациентов, аудиозаписи.
/// Полиморфная привязка через (OwnerEntityType, OwnerEntityId) — FK к конкретным таблицам не обеспечивается.
/// </summary>
public class Attachment
{
    public int Id { get; set; }

    [Required, MaxLength(256)] public string FileName { get; set; } = string.Empty;
    [Required, MaxLength(128)] public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }

    [Required, MaxLength(512)] public string StorageKey { get; set; } = string.Empty;

    [Required, MaxLength(64)] public string OwnerEntityType { get; set; } = string.Empty;
    public int OwnerEntityId { get; set; }

    public int? UploadedById { get; set; }
    public User? UploadedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}


/// <summary>
/// Журнал действий (Док.06 §25.4). Включает события из web-кабинета И из desktop/Player.
/// </summary>
public class AuditLog
{
    public long Id { get; set; }

    public AuditCategory Category { get; set; }

    /// <summary>Машинный код события: login_success, template_published, session_completed и т.п.</summary>
    [Required, MaxLength(64)] public string Action { get; set; } = string.Empty;

    public int? ActorUserId { get; set; }
    public User? ActorUser { get; set; }

    public int? OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public int? DeviceId { get; set; }
    public Device? Device { get; set; }

    public int? SessionId { get; set; }
    public Session? Session { get; set; }

    /// <summary>Тип объекта, к которому относится действие (Patient/Exercise/Template/...).</summary>
    [MaxLength(64)] public string? ObjectType { get; set; }
    public int? ObjectId { get; set; }

    /// <summary>Старое и новое значение (полное или diff).</summary>
    [Column(TypeName = "jsonb")] public string? OldValue { get; set; }
    [Column(TypeName = "jsonb")] public string? NewValue { get; set; }

    [MaxLength(64)]  public string? IpAddress { get; set; }
    [MaxLength(512)] public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}


// =============== Аутентификация =============================================

public class RefreshToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }

    [Required, MaxLength(128)] public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedReason { get; set; }

    [MaxLength(64)]  public string? CreatedByIp { get; set; }
    [MaxLength(512)] public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class PasswordResetToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }

    [Required, MaxLength(128)] public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }   // 24ч по US 1.1
    public bool Used { get; set; }
    public DateTime? UsedAt { get; set; }

    [MaxLength(64)] public string? RequestedFromIp { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class LoginAttempt
{
    public long Id { get; set; }

    [Required, MaxLength(64)] public string Login { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public User? User { get; set; }

    public bool Success { get; set; }
    [MaxLength(64)]  public string? IpAddress { get; set; }
    [MaxLength(512)] public string? UserAgent { get; set; }
    [MaxLength(128)] public string? FailureReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}


/// <summary>
/// Идемпотентные batch-загрузки телеметрии (Док.06 §27.4).
/// Запоминаем хеш+результат: повтор того же batch'а не должен создавать дубликаты.
/// </summary>
public class IdempotencyKey
{
    public long Id { get; set; }

    [Required, MaxLength(128)] public string Key { get; set; } = string.Empty;

    [Required, MaxLength(64)] public string Endpoint { get; set; } = string.Empty;

    public int? SessionId { get; set; }
    public Session? Session { get; set; }

    /// <summary>HTTP-статус, который вернули в первый раз.</summary>
    public int ResponseStatus { get; set; }

    /// <summary>Тело ответа (или хеш) — при повторе вернём то же.</summary>
    [Column(TypeName = "jsonb")] public string? ResponseBody { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(7);
}
