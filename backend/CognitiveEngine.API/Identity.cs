using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CognitiveEngine.API.Models.Enums;

namespace CognitiveEngine.API.Models;

// ============================================================================
//  Организация → Пользователи / Пациенты / Устройства / Шаблоны (Док.06 §25)
// ============================================================================
public class Organization
{
    public int Id { get; set; }

    [Required, MaxLength(256)] public string Name { get; set; } = string.Empty;

    public OrganizationType Type { get; set; } = OrganizationType.Other;

    [MaxLength(12)]  public string? Inn { get; set; }
    [MaxLength(512)] public string? Address { get; set; }
    [MaxLength(32)]  public string? Phone { get; set; }
    [MaxLength(256)] public string? Email { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<User> Users { get; set; } = new();
    public List<Patient> Patients { get; set; } = new();
    public List<Device> Devices { get; set; } = new();
    public List<ExerciseTemplate> Templates { get; set; } = new();
}


public class User
{
    public int Id { get; set; }

    /// <summary>3..32 символа, буквы/цифры/подчёркивания/точки. Уникален.</summary>
    [Required, MinLength(3), MaxLength(32)] public string Login { get; set; } = string.Empty;

    [Required, MaxLength(256), EmailAddress] public string Email { get; set; } = string.Empty;

    [MaxLength(128)] public string? FirstName { get; set; }
    [MaxLength(128)] public string? LastName { get; set; }
    [MaxLength(128)] public string? MiddleName { get; set; }
    [MaxLength(32)]  public string? Phone { get; set; }

    [Required, MaxLength(512)] public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Operator;

    public int OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public bool IsActive { get; set; } = true;
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }
    public DateTime? LastLoginAt { get; set; }

    [MaxLength(512)] public string? AvatarUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<RefreshToken> RefreshTokens { get; set; } = new();
}


/// <summary>
/// Карточка испытуемого (Док.05 §24, Док.06 §25.2).
/// Поддерживается режим деперсонализации: код-идентификатор взамен ФИО.
/// </summary>
public class Patient
{
    public int Id { get; set; }

    /// <summary>Код пациента (P-0041 и т.п.). Уникален в рамках организации.</summary>
    [Required, MaxLength(64)] public string Code { get; set; } = string.Empty;

    [MaxLength(128)] public string? FirstName { get; set; }
    [MaxLength(128)] public string? LastName { get; set; }
    [MaxLength(128)] public string? MiddleName { get; set; }

    public DateOnly? BirthDate { get; set; }
    public Gender? Gender { get; set; }

    /// <summary>Рост, см.</summary>
    [Column(TypeName = "numeric(5,2)")] public decimal? HeightCm { get; set; }
    /// <summary>Вес, кг.</summary>
    [Column(TypeName = "numeric(5,2)")] public decimal? WeightKg { get; set; }

    /// <summary>Текстовое описание ограничений и противопоказаний (поднимать руку только до плеча и т.п.).</summary>
    public string? Restrictions { get; set; }

    [MaxLength(256), EmailAddress] public string? Email { get; set; }
    [MaxLength(32)]  public string? Phone { get; set; }
    [MaxLength(512)] public string? AvatarUrl { get; set; }

    public int OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    /// <summary>Если у пациента есть ЛК — связанный User.</summary>
    public int? UserId { get; set; }
    public User? User { get; set; }

    /// <summary>true — данные пациента отображаются в интерфейсах в обезличенном виде (только Code).</summary>
    public bool IsDepersonalized { get; set; }

    /// <summary>
    /// Произвольные клинические/исследовательские поля под организацию: диагноз, виды спорта, успеваемость и т.п.
    /// JSONB — гибко без миграций.
    /// </summary>
    [Column(TypeName = "jsonb")] public string ProfileData { get; set; } = "{}";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<Consent> Consents { get; set; } = new();
    public List<Session> Sessions { get; set; } = new();
    public List<FreeTrace> FreeTraces { get; set; } = new();
    public List<DerivedTrack> DerivedTracks { get; set; } = new();
    public List<Comment> Comments { get; set; } = new();
}


public class Consent
{
    public int Id { get; set; }

    public int PatientId { get; set; }
    public Patient? Patient { get; set; }

    public ConsentType Type { get; set; }
    public bool Granted { get; set; }
    [MaxLength(32)] public string? AgreementVersion { get; set; }
    [MaxLength(64)] public string? IpAddress { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
