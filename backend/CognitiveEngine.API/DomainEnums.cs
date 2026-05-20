namespace CognitiveEngine.API.Models.Enums;

/// <summary>Роли (Док.06 §25.3).</summary>
public enum UserRole
{
    Operator   = 1,   // врач/оператор
    Patient    = 2,   // испытуемый
    Methodist  = 3,
    Admin      = 4,
    Researcher = 5
}

/// <summary>Тип учреждения. В Док.07 явных типов меньше — оставляю прежний классификатор для совместимости с внешними процессами.</summary>
public enum OrganizationType { Educational = 1, Sports = 2, Medical = 3, Neurofitness = 4, Other = 5 }

public enum Gender { Male = 1, Female = 2, Other = 3 }

/// <summary>Сторона тела для справочника BodyPoints (центр / лево / право).</summary>
public enum BodySide { Center = 1, Left = 2, Right = 3 }

/// <summary>Группа точек тела для UX-сегментации (Док.02 §5.1).</summary>
public enum BodyGroup { CentralHead = 1, CentralChest = 2, CentralBody = 3, RightArm = 4, LeftArm = 5, RightLeg = 6, LeftLeg = 7 }

/// <summary>Тип физической рабочей зоны (Док.01 §3, Док.04 §18).</summary>
public enum DeviceType { VerticalPanel100 = 1, VideoProjection = 2, OtherPanel = 3, Emulator = 4 }

public enum DeviceStatus { Active = 1, Inactive = 2, Maintenance = 3, Decommissioned = 4 }

/// <summary>Источник координат курсора (Док.05 §20.1).</summary>
public enum TrackerType { ViveTracker = 1, ImuSensor = 2, OpticalCamera = 3, Emulator = 4 }

public enum TrackerStatus { Available = 1, Bound = 2, SignalLost = 3, Recalibrate = 4, Faulty = 5 }

/// <summary>Статус публикации шаблона (Док.04 §16).</summary>
public enum TemplateStatus { Draft = 1, Published = 2, Archived = 3, Withdrawn = 4 }

/// <summary>Уровень шаблона (Док.04 §16.1).</summary>
public enum TemplateScope { System = 1, Organization = 2, Personal = 3 }

/// <summary>Статус конкретного назначения упражнения пациенту.</summary>
public enum ExerciseStatus { Draft = 1, Validated = 2, Ready = 3, Running = 4, Completed = 5, Aborted = 6, Failed = 7 }

/// <summary>Состояния Player (Док.05 §20.2).</summary>
public enum SessionStatus { ConfigLoaded = 1, CalibrationCheck = 2, Ready = 3, Countdown = 4, Running = 5, Paused = 6, Completed = 7, Aborted = 8, Error = 9, PartiallySaved = 10 }

/// <summary>Тип события в SessionEvents (Док.05 §21.3).</summary>
public enum SessionEventType
{
    SessionStarted = 1, CountdownStarted = 2, PacerStarted = 3,
    CursorEnteredZone = 10, CursorLeftZone = 11, CorridorExit = 12,
    TargetHit = 20, TargetMissed = 21,
    PacerLag = 30, PacerLead = 31,
    SignalLost = 40, SignalRestored = 41,
    ImuDriftWarning = 50, ImuReset = 51,
    SessionPaused = 60, SessionResumed = 61, SessionCompleted = 62,
    OperatorComment = 70
}

/// <summary>Источник sample-значений: сырые от датчика или после калибровки/фильтра.</summary>
public enum SampleKind { Cursor = 1, Pacer = 2 }

public enum SignalStatus { Ok = 1, LowQuality = 2, Lost = 3, Interpolated = 4 }

/// <summary>Тип согласия (152-ФЗ).</summary>
public enum ConsentType { PersonalData = 1, Depersonalization = 2, ResearchUse = 3, Marketing = 4 }

/// <summary>Состояние FreeTrace (Док.04 §15.2).</summary>
public enum FreeTraceStatus { Recording = 1, Frozen = 2, Converted = 3, Archived = 4 }

public enum CalibrationMode { Screen = 1, Projection = 2, Emulator = 3 }

/// <summary>Уровень метрики (Док.05 §22.1).</summary>
public enum MetricScope { Session = 1, Exercise = 2, BodyPointZone = 3, BodyPoint = 4, Track = 5, Segment = 6, Shape = 7, Corridor = 8, Pacer = 9, Dynamics = 10 }

public enum MetricQuality { Valid = 1, Partial = 2, Invalid = 3 }

public enum ReportType { Session = 1, Exercise = 2, BodyPointZone = 3, Corridor = 4, PacerSync = 5, FreeTrace = 6, Calibration = 7, Dynamics = 8 }

public enum ExportFormat { Pdf = 1, Csv = 2, Xlsx = 3, Json = 4 }
public enum ExportStatus { Pending = 1, Processing = 2, Ready = 3, Failed = 4 }

/// <summary>Категории действий для аудита (Док.06 §25.4).</summary>
public enum AuditCategory { Auth = 1, Admin = 2, Patient = 3, Exercise = 4, Session = 5, Tracking = 6, Export = 7 }
