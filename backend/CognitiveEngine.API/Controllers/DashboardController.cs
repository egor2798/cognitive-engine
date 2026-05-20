using CognitiveEngine.API.Dtos;
using CognitiveEngine.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CognitiveEngine.API.Controllers;

[ApiController]
public sealed class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db) => _db = db;

    [HttpGet("api/dashboard")]
    public async Task<ActionResult<DashboardDto>> GetDashboard(CancellationToken ct)
    {
        var latest = await _db.Sessions
            .AsNoTracking()
            .Include(x => x.Exercise)
            .Include(x => x.Patient)
            .Include(x => x.Metrics)
            .OrderByDescending(x => x.StartedAt)
            .FirstOrDefaultAsync(ct);

        var latestDto = latest == null ? null : new SessionListDto(
            latest.Id,
            latest.ExerciseId,
            latest.Exercise?.Name ?? $"Exercise #{latest.ExerciseId}",
            latest.PatientId,
            latest.Patient?.Code ?? $"P-{latest.PatientId}",
            latest.StartedAt,
            latest.FinishedAt,
            (int)latest.Status,
            latest.FinishedAt.HasValue ? (decimal)(latest.FinishedAt.Value - latest.StartedAt).TotalSeconds : null,
            latest.Metrics.Select(m => new MetricDto(m.Code, m.Value, m.Unit, (int)m.Quality)).ToList()
        );

        return Ok(new DashboardDto(
            await _db.BodyPoints.CountAsync(ct),
            await _db.Patients.CountAsync(ct),
            await _db.Exercises.CountAsync(ct),
            await _db.ExerciseTemplates.CountAsync(ct),
            await _db.Sessions.CountAsync(ct),
            await _db.SessionSamples.CountAsync(ct),
            await _db.SessionEvents.CountAsync(ct),
            await _db.Metrics.CountAsync(ct),
            latestDto
        ));
    }
}
