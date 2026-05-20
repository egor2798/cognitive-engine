using System.Security.Cryptography;
using System.Text;
using CognitiveEngine.API.Dtos;
using CognitiveEngine.API.Models;
using CognitiveEngine.API.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CognitiveEngine.API.Controllers;

[ApiController]
public sealed class ExercisesController : ControllerBase
{
    private readonly AppDbContext _db;

    public ExercisesController(AppDbContext db) => _db = db;

    [HttpGet("api/exercise-templates")]
    public async Task<ActionResult<IReadOnlyList<ExerciseTemplateDto>>> GetTemplates(CancellationToken ct)
    {
        var items = await _db.ExerciseTemplates
            .AsNoTracking()
            .OrderByDescending(x => x.UpdatedAt)
            .Select(x => new ExerciseTemplateDto(
                x.Id,
                x.Name,
                x.Version,
                (int)x.Scope,
                (int)x.Status,
                x.Description,
                x.CreatedAt,
                x.UpdatedAt))
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpGet("api/exercises")]
    public async Task<ActionResult<IReadOnlyList<ExerciseDto>>> GetExercises(CancellationToken ct)
    {
        var items = await _db.Exercises
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new ExerciseDto(
                x.Id,
                x.Name,
                x.TemplateId,
                x.PatientId,
                (int)x.Status,
                x.ScheduledAt,
                x.CreatedAt))
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPost("api/exercise-templates/from-unity")]
    public async Task<ActionResult<ExerciseTemplateDto>> CreateTemplateFromUnity(
        [FromBody] CreateExerciseTemplateRequest request,
        CancellationToken ct)
    {
        var config = request.ConfigJson.HasValue
            ? request.ConfigJson.Value.GetRawText()
            : "{}";

        var template = new ExerciseTemplate
        {
            Name = string.IsNullOrWhiteSpace(request.Name) ? "Unity Exercise" : request.Name.Trim(),
            Description = request.Description,
            Version = string.IsNullOrWhiteSpace(request.Version) ? "1.0.0" : request.Version.Trim(),
            Scope = TemplateScope.Personal,
            Status = TemplateStatus.Draft,
            ConfigJson = config,
            ConfigHash = Sha256(config),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.ExerciseTemplates.Add(template);
        await _db.SaveChangesAsync(ct);

        return Ok(new ExerciseTemplateDto(
            template.Id,
            template.Name,
            template.Version,
            (int)template.Scope,
            (int)template.Status,
            template.Description,
            template.CreatedAt,
            template.UpdatedAt));
    }

    private static string Sha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
