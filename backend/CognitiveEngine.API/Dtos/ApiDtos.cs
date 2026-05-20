using System.Text.Json;

namespace CognitiveEngine.API.Dtos;

public sealed record BodyPointDto(
    int Id,
    string Code,
    string Name,
    int Side,
    int Group,
    int OrderIndex
);

public sealed record ExerciseTemplateDto(
    int Id,
    string Name,
    string Version,
    int Scope,
    int Status,
    string? Description,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public sealed record ExerciseDto(
    int Id,
    string Name,
    int TemplateId,
    int PatientId,
    int Status,
    DateTime? ScheduledAt,
    DateTime CreatedAt
);

public sealed record MetricDto(
    string Code,
    decimal Value,
    string Unit,
    int Quality
);

public sealed record SessionListDto(
    int Id,
    int ExerciseId,
    string ExerciseName,
    int PatientId,
    string PatientCode,
    DateTime StartedAt,
    DateTime? FinishedAt,
    int Status,
    decimal? DurationSec,
    IReadOnlyList<MetricDto> Metrics
);

public sealed record DashboardDto(
    int BodyPoints,
    int Patients,
    int Exercises,
    int Templates,
    int Sessions,
    int Samples,
    int Events,
    int Metrics,
    SessionListDto? LatestSession
);

public sealed class CreateExerciseTemplateRequest
{
    public string Name { get; set; } = "New Exercise";
    public string? Description { get; set; }
    public string Version { get; set; } = "1.0.0";
    public JsonElement? ConfigJson { get; set; }
}

public sealed class UnitySessionImportRequest
{
    public int? ExerciseId { get; set; }
    public int? PatientId { get; set; }
    public int? DeviceId { get; set; }
    public int? CalibrationProfileId { get; set; }
    public int? OperatorId { get; set; }
    public string? Source { get; set; }
    public JsonElement ResultJson { get; set; }
}
