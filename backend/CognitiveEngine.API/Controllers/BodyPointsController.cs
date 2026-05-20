using CognitiveEngine.API.Dtos;
using CognitiveEngine.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CognitiveEngine.API.Controllers;

[ApiController]
[Route("api/body-points")]
public sealed class BodyPointsController : ControllerBase
{
    private readonly AppDbContext _db;

    public BodyPointsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BodyPointDto>>> GetBodyPoints(CancellationToken ct)
    {
        var items = await _db.BodyPoints
            .AsNoTracking()
            .OrderBy(x => x.OrderIndex)
            .Select(x => new BodyPointDto(
                x.Id,
                x.Code,
                x.Name,
                (int)x.Side,
                (int)x.Group,
                x.OrderIndex))
            .ToListAsync(ct);

        return Ok(items);
    }
}
