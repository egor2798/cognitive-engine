using System.Text.Json;
using CognitiveEngine.API.Dtos;
using CognitiveEngine.API.Models;
using CognitiveEngine.API.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CognitiveEngine.API.Controllers;

[ApiController]
[Route("api/patients")]
public sealed class PatientsController : ControllerBase
{
    private readonly AppDbContext _db;

    public PatientsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PatientDto>>> GetPatients(CancellationToken ct)
    {
        var patients = await _db.Patients
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Code)
            .ToListAsync(ct);

        return Ok(patients.Select(ToDto).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PatientDto>> GetPatient(int id, CancellationToken ct)
    {
        var patient = await _db.Patients.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return patient == null ? NotFound() : Ok(ToDto(patient));
    }

    [HttpPost]
    public async Task<ActionResult<PatientDto>> CreatePatient([FromBody] UpsertPatientRequest request, CancellationToken ct)
    {
        var organizationId = await EnsureDefaultOrganization(ct);
        var code = string.IsNullOrWhiteSpace(request.Code)
            ? await GeneratePatientCode(organizationId, ct)
            : request.Code.Trim();

        var duplicate = await _db.Patients.AnyAsync(x => x.OrganizationId == organizationId && x.Code == code, ct);
        if (duplicate)
            return Conflict($"Patient code already exists: {code}");

        var patient = new Patient
        {
            Code = code,
            FirstName = request.FirstName?.Trim(),
            LastName = request.LastName?.Trim(),
            MiddleName = request.MiddleName?.Trim(),
            BirthDate = BuildBirthDate(request),
            Gender = ParseGender(request.Gender),
            HeightCm = request.HeightCm,
            WeightKg = request.WeightKg,
            Restrictions = request.Restrictions,
            Email = request.Email,
            Phone = request.Phone,
            OrganizationId = organizationId,
            IsDepersonalized = request.IsDepersonalized ?? false,
            ProfileData = BuildProfileData(request.Diagnosis),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Patients.Add(patient);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetPatient), new { id = patient.Id }, ToDto(patient));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PatientDto>> UpdatePatient(int id, [FromBody] UpsertPatientRequest request, CancellationToken ct)
    {
        var patient = await _db.Patients.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (patient == null)
            return NotFound();

        if (!string.IsNullOrWhiteSpace(request.Code) && request.Code.Trim() != patient.Code)
        {
            var code = request.Code.Trim();
            var duplicate = await _db.Patients.AnyAsync(x => x.OrganizationId == patient.OrganizationId && x.Code == code && x.Id != id, ct);
            if (duplicate)
                return Conflict($"Patient code already exists: {code}");
            patient.Code = code;
        }

        patient.FirstName = request.FirstName?.Trim();
        patient.LastName = request.LastName?.Trim();
        patient.MiddleName = request.MiddleName?.Trim();
        patient.BirthDate = BuildBirthDate(request);
        patient.Gender = ParseGender(request.Gender);
        patient.HeightCm = request.HeightCm;
        patient.WeightKg = request.WeightKg;
        patient.Restrictions = request.Restrictions;
        patient.Email = request.Email;
        patient.Phone = request.Phone;
        patient.IsDepersonalized = request.IsDepersonalized ?? patient.IsDepersonalized;
        patient.ProfileData = BuildProfileData(request.Diagnosis);
        patient.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Ok(ToDto(patient));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeletePatient(int id, CancellationToken ct)
    {
        var patient = await _db.Patients.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (patient == null)
            return NotFound();

        _db.Patients.Remove(patient);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task<int> EnsureDefaultOrganization(CancellationToken ct)
    {
        var org = await _db.Organizations.OrderBy(x => x.Id).FirstOrDefaultAsync(ct);
        if (org != null)
            return org.Id;

        org = new Organization
        {
            Name = "Cognitive Engine Demo Clinic",
            Type = OrganizationType.Medical,
            Email = "demo@cognitive.local",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Organizations.Add(org);
        await _db.SaveChangesAsync(ct);
        return org.Id;
    }

    private async Task<string> GeneratePatientCode(int organizationId, CancellationToken ct)
    {
        var count = await _db.Patients.CountAsync(x => x.OrganizationId == organizationId, ct);
        return $"P-{count + 1:0000}";
    }

    private static PatientDto ToDto(Patient p)
    {
        var name = string.Join(" ", new[] { p.LastName, p.FirstName, p.MiddleName }
            .Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
        if (string.IsNullOrWhiteSpace(name))
            name = p.Code;

        return new PatientDto(
            p.Id,
            p.Code,
            p.IsDepersonalized ? p.Code : name,
            CalculateAge(p.BirthDate),
            ExtractDiagnosis(p.ProfileData),
            p.Phone,
            p.Email,
            p.Gender?.ToString(),
            p.HeightCm,
            p.WeightKg,
            p.Restrictions,
            p.IsDepersonalized,
            p.CreatedAt
        );
    }

    private static int? CalculateAge(DateOnly? birthDate)
    {
        if (birthDate == null)
            return null;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - birthDate.Value.Year;
        if (birthDate.Value > today.AddYears(-age))
            age--;
        return age;
    }

    private static DateOnly? BuildBirthDate(UpsertPatientRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.BirthDate) && DateOnly.TryParse(request.BirthDate, out var parsed))
            return parsed;

        if (request.Age is > 0 and < 120)
            return DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-request.Age.Value));

        return null;
    }

    private static Gender? ParseGender(string? value)
    {
        var gender = (value ?? "").Trim().ToLowerInvariant();
        return gender switch
        {
            "male" or "мужской" or "м" => Gender.Male,
            "female" or "женский" or "ж" => Gender.Female,
            "other" or "другое" => Gender.Other,
            _ => null
        };
    }

    private static string BuildProfileData(string? diagnosis)
    {
        var payload = new Dictionary<string, string?>
        {
            ["diagnosis"] = diagnosis ?? ""
        };
        return JsonSerializer.Serialize(payload);
    }

    private static string? ExtractDiagnosis(string? profileData)
    {
        if (string.IsNullOrWhiteSpace(profileData))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(profileData);
            return doc.RootElement.TryGetProperty("diagnosis", out var d) ? d.GetString() : null;
        }
        catch
        {
            return null;
        }
    }
}
