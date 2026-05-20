using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CognitiveEngine.API.Models.Enums;

namespace CognitiveEngine.API.Models;

// ============================================================================
//  Аппаратный слой: устройство (вертикальная панель/проекция),
//  трекеры (Vive/IMU/камеры), справочник 11 точек тела, профиль калибровки.
//  Док.01 §3, Док.04 §18, Док.06 §25.
// ============================================================================

/// <summary>
/// Физическое рабочее место: 100" вертикальная панель, видеопроекция и т.п.
/// Без активного устройства и профиля калибровки сессия не запускается (Док.06 QA-DEV-01).
/// </summary>
public class Device
{
    public int Id { get; set; }

    [Required, MaxLength(128)] public string Name { get; set; } = string.Empty;
    public DeviceType Type { get; set; } = DeviceType.VerticalPanel100;

    /// <summary>Кабинет / помещение / локация.</summary>
    [MaxLength(256)] public string? Location { get; set; }

    /// <summary>Серийный номер / инвентарный код.</summary>
    [MaxLength(128)] public string? SerialNumber { get; set; }

    public int WidthPx { get; set; }
    public int HeightPx { get; set; }

    /// <summary>true — рабочая ориентация панели вертикальная (portrait).</summary>
    public bool IsPortrait { get; set; } = true;

    /// <summary>Физические размеры рабочей зоны, мм.</summary>
    [Column(TypeName = "numeric(8,2)")] public decimal? PhysicalWidthMm { get; set; }
    [Column(TypeName = "numeric(8,2)")] public decimal? PhysicalHeightMm { get; set; }

    public DeviceStatus Status { get; set; } = DeviceStatus.Active;

    public int OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    /// <summary>Текущий активный профиль калибровки.</summary>
    public int? ActiveCalibrationProfileId { get; set; }
    public CalibrationProfile? ActiveCalibrationProfile { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<Tracker> Trackers { get; set; } = new();
    public List<CalibrationProfile> CalibrationProfiles { get; set; } = new();
}


/// <summary>
/// Трекер (Vive/IMU/камера/эмулятор). Может быть закреплён за конкретной точкой тела
/// либо назначаться динамически в рамках сессии (Док.04 §19).
/// </summary>
public class Tracker
{
    public int Id { get; set; }

    [Required, MaxLength(128)] public string Name { get; set; } = string.Empty;
    public TrackerType Type { get; set; } = TrackerType.ImuSensor;
    [MaxLength(128)] public string? SerialNumber { get; set; }

    public int DeviceId { get; set; }
    public Device? Device { get; set; }

    /// <summary>Закреплённая точка тела (опц.). null = свободный, привязка делается в сессии.</summary>
    public int? BodyPointId { get; set; }
    public BodyPoint? BodyPoint { get; set; }

    public TrackerStatus Status { get; set; } = TrackerStatus.Available;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}


/// <summary>
/// Справочник 11 точек тела (Док.02 §5.1). Сидится в БД при инициализации,
/// после этого записи иммутабельны: на BP-XX ссылаются миллионы samples.
/// </summary>
public class BodyPoint
{
    public int Id { get; set; }

    /// <summary>Код, например "BP-05" или "right_forearm".</summary>
    [Required, MaxLength(32)] public string Code { get; set; } = string.Empty;

    [Required, MaxLength(128)] public string Name { get; set; } = string.Empty;

    public BodySide Side { get; set; } = BodySide.Center;
    public BodyGroup Group { get; set; } = BodyGroup.CentralChest;

    /// <summary>Порядок отображения в UI 1..11.</summary>
    public int OrderIndex { get; set; }
}


/// <summary>
/// Профиль калибровки (Док.04 §18.4). Связывает устройство, физический масштаб,
/// 4 угла рабочей зоны, ростовые настройки и схему секций тела.
/// Используется как иммутабельная привязка: сессия хранит calibrationProfileId
/// и snapshot его параметров на момент запуска.
/// </summary>
public class CalibrationProfile
{
    public int Id { get; set; }

    [Required, MaxLength(128)] public string Name { get; set; } = string.Empty;

    public int DeviceId { get; set; }
    public Device? Device { get; set; }

    public CalibrationMode Mode { get; set; } = CalibrationMode.Screen;

    /// <summary>Опционально: профиль создан под конкретного пациента.</summary>
    public int? PatientId { get; set; }
    public Patient? Patient { get; set; }

    public int WidthPx { get; set; }
    public int HeightPx { get; set; }

    [Column(TypeName = "numeric(8,2)")] public decimal? PhysicalWidthMm { get; set; }
    [Column(TypeName = "numeric(8,2)")] public decimal? PhysicalHeightMm { get; set; }

    [Column(TypeName = "numeric(10,5)")] public decimal? ScaleX { get; set; }   // px → mm
    [Column(TypeName = "numeric(10,5)")] public decimal? ScaleY { get; set; }
    [Column(TypeName = "numeric(8,2)")]  public decimal? OffsetXMm { get; set; }
    [Column(TypeName = "numeric(8,2)")]  public decimal? OffsetYMm { get; set; }

    [Column(TypeName = "numeric(5,2)")] public decimal? PatientHeightCm { get; set; }

    /// <summary>
    /// Полная карта параметров: 4 угла, схема секций тела (центр/руки/ноги),
    /// сопоставление trackerId↔bodyPointId, состояние IMU zero pose, параметры фильтра.
    /// </summary>
    [Column(TypeName = "jsonb")] public string ConfigJson { get; set; } = "{}";

    public bool IsActive { get; set; } = true;
    public DateTime? ValidUntil { get; set; }

    public int? CreatedById { get; set; }
    public User? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
