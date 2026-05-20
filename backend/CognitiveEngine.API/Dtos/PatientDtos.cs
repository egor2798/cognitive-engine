namespace CognitiveEngine.API.Dtos;

public sealed record PatientDto(
    int Id,
    string Code,
    string Name,
    int? Age,
    string? Diagnosis,
    string? Phone,
    string? Email,
    string? Gender,
    decimal? HeightCm,
    decimal? WeightKg,
    string? Restrictions,
    bool IsDepersonalized,
    DateTime CreatedAt
);

public sealed record UpsertPatientRequest(
    string? Code,
    string? FirstName,
    string? LastName,
    string? MiddleName,
    int? Age,
    string? BirthDate,
    string? Diagnosis,
    string? Phone,
    string? Email,
    string? Gender,
    decimal? HeightCm,
    decimal? WeightKg,
    string? Restrictions,
    bool? IsDepersonalized
);
