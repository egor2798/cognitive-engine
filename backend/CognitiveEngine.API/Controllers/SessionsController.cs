using System.Text.Json;
using CognitiveEngine.API.Dtos;
using CognitiveEngine.API.Models;
using CognitiveEngine.API.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CognitiveEngine.API.Controllers;

[ApiController]
public sealed class SessionsController : ControllerBase
{
    private readonly AppDbContext _db;

    public SessionsController(AppDbContext db) => _db = db;

    [HttpGet("api/sessions")]
    public async Task<ActionResult<IReadOnlyList<SessionListDto>>> GetSessions(CancellationToken ct)
    {
        var sessions = await _db.Sessions
            .AsNoTracking()
            .Include(x => x.Exercise)
            .Include(x => x.Patient)
            .Include(x => x.Metrics)
            .OrderByDescending(x => x.StartedAt)
            .Take(50)
            .ToListAsync(ct);

        return Ok(sessions.Select(ToListDto).ToList());
    }

    [HttpGet("api/sessions/{id:int}")]
    public async Task<ActionResult<object>> GetSession(int id, CancellationToken ct)
    {
        var session = await _db.Sessions
            .AsNoTracking()
            .Include(x => x.Exercise)
            .Include(x => x.Patient)
            .Include(x => x.Metrics)
            .Include(x => x.Events)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (session == null)
            return NotFound();

        var samples = await _db.SessionSamples
            .AsNoTracking()
            .Where(x => x.SessionId == id)
            .OrderBy(x => x.Kind)
            .ThenBy(x => x.SampleIndex)
            .Take(500)
            .Select(x => new
            {
                x.Kind,
                x.SampleIndex,
                x.T,
                x.SlotIndex,
                x.CursorRef,
                x.PacerRef,
                x.TrackRef,
                x.BodyPointZoneRef,
                x.BodyPointId,
                x.Xmm,
                x.Ymm,
                x.Speed,
                x.InsideCorridor,
                x.DistanceToTrackCenterMm,
                x.DistanceToPacerMm,
                x.SignalStatus,
                x.Confidence,
                x.ExtraJson
            })
            .ToListAsync(ct);

        return Ok(new
        {
            session = ToListDto(session),
            metrics = session.Metrics.Select(m => new MetricDto(m.Code, m.Value, m.Unit, (int)m.Quality)),
            events = session.Events.OrderBy(e => e.T).Select(e => new
            {
                e.Id,
                e.Type,
                e.T,
                e.CursorRef,
                e.PacerRef,
                e.TrackRef,
                e.BodyPointZoneRef,
                e.ShapeRef,
                e.Payload
            }),
            samples
        });
    }

    [HttpPost("api/sessions/import-json")]
    public async Task<ActionResult<object>> ImportUnitySession([FromBody] UnitySessionImportRequest request, CancellationToken ct)
    {
        if (request.ResultJson.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            return BadRequest("ResultJson is required.");

        var exercise = request.ExerciseId.HasValue
            ? await _db.Exercises.Include(x => x.Template).FirstOrDefaultAsync(x => x.Id == request.ExerciseId.Value, ct)
            : await _db.Exercises.Include(x => x.Template).OrderBy(x => x.Id).FirstOrDefaultAsync(ct);

        if (exercise == null)
            return BadRequest("No exercise found. Run seed.sql first or create exercise before importing session.");

        var patientId = request.PatientId ?? exercise.PatientId;
        var deviceId = request.DeviceId ?? await _db.Devices.Select(x => x.Id).FirstOrDefaultAsync(ct);
        var calibrationProfileId = request.CalibrationProfileId ?? await _db.CalibrationProfiles.Select(x => x.Id).FirstOrDefaultAsync(ct);

        if (deviceId == 0 || calibrationProfileId == 0)
            return BadRequest("No device/calibration profile found. Run seed.sql first.");

        var root = request.ResultJson;
        var durationSec = TryGetNumberRecursive(root, "durationSec")
            ?? TryGetNumberRecursive(root, "sessionTotalDurationSec")
            ?? TryGetNumberRecursive(root, "totalDurationSec")
            ?? 60m;

        var startedAt = DateTime.UtcNow.AddSeconds(-(double)durationSec);
        var finishedAt = DateTime.UtcNow;

        var configSnapshot = TryGetObjectRaw(root, "exerciseConfigSnapshot")
            ?? TryGetObjectRaw(root, "exercise")
            ?? "{}";

        var session = new Session
        {
            ExerciseId = exercise.Id,
            PatientId = patientId,
            OperatorId = request.OperatorId,
            DeviceId = deviceId,
            CalibrationProfileId = calibrationProfileId,
            TemplateVersion = exercise.Template?.Version,
            StartedAt = startedAt,
            FinishedAt = finishedAt,
            Status = SessionStatus.Completed,
            ExerciseConfigSnapshot = configSnapshot,
            CalibrationSnapshot = TryGetObjectRaw(root, "calibrationSnapshot") ?? "{}",
            OperatorNote = $"Imported from Unity JSON. Source={request.Source ?? "manual"}",
            AiSummary = BuildSummary(root),
            WasOffline = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Sessions.Add(session);
        await _db.SaveChangesAsync(ct);

        AddMetrics(session.Id, root);
        AddEvents(session.Id, root);
        AddSamples(session.Id, root);

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            session.Id,
            message = "Session imported",
            metrics = await _db.Metrics.CountAsync(x => x.SessionId == session.Id, ct),
            events = await _db.SessionEvents.CountAsync(x => x.SessionId == session.Id, ct),
            samples = await _db.SessionSamples.CountAsync(x => x.SessionId == session.Id, ct)
        });
    }

    [HttpPost("api/demo/seed-session")]
    public async Task<ActionResult<object>> SeedDemoSession(CancellationToken ct)
    {
        var exercise = await _db.Exercises.Include(x => x.Template).OrderBy(x => x.Id).FirstOrDefaultAsync(ct);
        if (exercise == null)
            return BadRequest("No exercise found. Run seed.sql first.");

        var deviceId = await _db.Devices.Select(x => x.Id).FirstOrDefaultAsync(ct);
        var calibrationId = await _db.CalibrationProfiles.Select(x => x.Id).FirstOrDefaultAsync(ct);
        if (deviceId == 0 || calibrationId == 0)
            return BadRequest("No device/calibration profile found. Run seed.sql first.");

        var now = DateTime.UtcNow;
        var session = new Session
        {
            ExerciseId = exercise.Id,
            PatientId = exercise.PatientId,
            OperatorId = 2,
            DeviceId = deviceId,
            CalibrationProfileId = calibrationId,
            TemplateVersion = exercise.Template?.Version,
            StartedAt = now.AddSeconds(-60),
            FinishedAt = now,
            Status = SessionStatus.Completed,
            ExerciseConfigSnapshot = exercise.Template?.ConfigJson ?? "{}",
            CalibrationSnapshot = "{\"workspaceWidthMm\":1000,\"workspaceHeightMm\":600,\"worldToMm\":125}",
            OperatorNote = "Demo session generated for API/web presentation.",
            AiSummary = "Демо-сессия: объединённый маршрут, пейсмейкер, курсор, метрики в мм и мм/с.",
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Sessions.Add(session);
        await _db.SaveChangesAsync(ct);

        AddMetric(session.Id, "meanDeviationMm", 9.59m, "mm");
        AddMetric(session.Id, "maxDeviationMm", 74.04m, "mm");
        AddMetric(session.Id, "rmseDeviationMm", 12.83m, "mm");
        AddMetric(session.Id, "timeOutsideSec", 0.00m, "s");
        AddMetric(session.Id, "outsidePercent", 0.00m, "%");
        AddMetric(session.Id, "outsideEpisodesCount", 0m, "count");
        AddMetric(session.Id, "lagTimeSec", 11.41m, "s");
        AddMetric(session.Id, "leadTimeSec", 10.01m, "s");
        AddMetric(session.Id, "meanPointerSpeedMmS", 89.70m, "mm/s");
        AddMetric(session.Id, "maxPointerSpeedMmS", 201.31m, "mm/s");
        AddMetric(session.Id, "pacemakerPathLengthMm", 4490.67m, "mm");
        AddMetric(session.Id, "meanPacemakerSpeedMmS", 74.84m, "mm/s");

        _db.SessionEvents.AddRange(
            DemoEvent(session.Id, SessionEventType.SessionStarted, 0m, "{\"message\":\"Сессия начата\"}"),
            DemoEvent(session.Id, SessionEventType.PacerStarted, 0.2m, "{\"message\":\"Пейсмейкер включён\"}"),
            DemoEvent(session.Id, SessionEventType.SessionCompleted, 60m, "{\"message\":\"Сессия завершена\"}")
        );

        for (var i = 0; i <= 60; i += 3)
        {
            var t = i;
            var x = -250m + i * 8m;
            var y = 80m + (decimal)Math.Sin(i / 8.0) * 60m;
            _db.SessionSamples.Add(new SessionSample
            {
                SessionId = session.Id,
                Kind = SampleKind.Cursor,
                SampleIndex = i / 3,
                T = t,
                SlotIndex = 1,
                CursorRef = "cursor_1",
                TrackRef = "composite_track_1",
                BodyPointZoneRef = "zone_right_forearm",
                BodyPointId = 5,
                Xmm = Math.Round(x, 2),
                Ymm = Math.Round(y, 2),
                Speed = 90m,
                InsideCorridor = true,
                DistanceToTrackCenterMm = 9.59m,
                SignalStatus = SignalStatus.Ok,
                Confidence = 1.0m,
                ExtraJson = "{\"source\":\"demo\",\"trackerId\":\"WT901_01\"}"
            });
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new { session.Id, message = "Demo session created" });
    }

    private static SessionListDto ToListDto(Session session)
    {
        var duration = session.FinishedAt.HasValue
            ? (decimal)(session.FinishedAt.Value - session.StartedAt).TotalSeconds
            : (decimal?)null;

        return new SessionListDto(
            session.Id,
            session.ExerciseId,
            session.Exercise?.Name ?? $"Exercise #{session.ExerciseId}",
            session.PatientId,
            session.Patient?.Code ?? $"P-{session.PatientId}",
            session.StartedAt,
            session.FinishedAt,
            (int)session.Status,
            duration,
            session.Metrics
                .OrderBy(x => x.Code)
                .Select(m => new MetricDto(m.Code, m.Value, m.Unit, (int)m.Quality))
                .ToList()
        );
    }

    private void AddMetrics(int sessionId, JsonElement root)
    {
        var metrics = new Dictionary<string, string>
        {
            ["durationSec"] = "s",
            ["meanDeviationMm"] = "mm",
            ["maxDeviationMm"] = "mm",
            ["rmseDeviationMm"] = "mm",
            ["timeInsideSec"] = "s",
            ["timeOutsideSec"] = "s",
            ["outsidePercent"] = "%",
            ["outsideEpisodesCount"] = "count",
            ["lagTimeSec"] = "s",
            ["leadTimeSec"] = "s",
            ["meanPointerSpeedMmS"] = "mm/s",
            ["maxPointerSpeedMmS"] = "mm/s",
            ["pointerPathLengthMm"] = "mm",
            ["pacemakerPathLengthMm"] = "mm",
            ["meanPacemakerSpeedMmS"] = "mm/s",
            ["maxPacemakerSpeedMmS"] = "mm/s"
        };

        foreach (var (code, unit) in metrics)
        {
            var value = TryGetNumberRecursive(root, code);
            if (value.HasValue)
                AddMetric(sessionId, code, value.Value, unit);
        }
    }

    private void AddMetric(int sessionId, string code, decimal value, string unit)
    {
        _db.Metrics.Add(new Metric
        {
            SessionId = sessionId,
            Scope = MetricScope.Session,
            Code = code,
            Value = value,
            Unit = unit,
            Quality = MetricQuality.Valid,
            AlgorithmVersion = "unity-prototype-1.0",
            CreatedAt = DateTime.UtcNow
        });
    }

    private void AddEvents(int sessionId, JsonElement root)
    {
        var events = TryGetArray(root, "events");
        if (events == null) return;

        var index = 0;
        foreach (var e in events.Value.EnumerateArray())
        {
            if (index++ > 1000) break;

            var code = TryGetString(e, "type") ?? TryGetString(e, "code") ?? TryGetString(e, "eventType") ?? "operator_comment";
            var t = TryGetNumber(e, "timeSec") ?? TryGetNumber(e, "t") ?? 0m;

            _db.SessionEvents.Add(new SessionEvent
            {
                SessionId = sessionId,
                Type = MapEventType(code),
                T = t,
                CursorRef = TryGetString(e, "cursorId") ?? TryGetString(e, "cursorRef"),
                PacerRef = TryGetString(e, "pacerId") ?? TryGetString(e, "pacerRef"),
                TrackRef = TryGetString(e, "trackId") ?? TryGetString(e, "trackRef"),
                BodyPointZoneRef = TryGetString(e, "bodyPointZoneId") ?? TryGetString(e, "bodyPointZoneRef"),
                ShapeRef = TryGetString(e, "shapeId") ?? TryGetString(e, "shapeRef"),
                Payload = e.GetRawText()
            });
        }
    }

    private void AddSamples(int sessionId, JsonElement root)
    {
        AddSampleArray(sessionId, root, "cursorSamples", SampleKind.Cursor);
        AddSampleArray(sessionId, root, "samples", SampleKind.Cursor);
        AddSampleArray(sessionId, root, "pacerSamples", SampleKind.Pacer);
    }

    private void AddSampleArray(int sessionId, JsonElement root, string propertyName, SampleKind kind)
    {
        var arr = TryGetArray(root, propertyName);
        if (arr == null) return;

        long i = 0;
        foreach (var s in arr.Value.EnumerateArray())
        {
            if (i >= 5000) break;
            var sample = new SessionSample
            {
                SessionId = sessionId,
                Kind = kind,
                SampleIndex = i,
                T = TryGetNumber(s, "timeSec") ?? TryGetNumber(s, "t") ?? TryGetNumber(s, "timestampSec") ?? i,
                SlotIndex = 1,
                CursorRef = TryGetString(s, "cursorId") ?? TryGetString(s, "cursorRef"),
                PacerRef = TryGetString(s, "pacerId") ?? TryGetString(s, "pacerRef"),
                TrackRef = TryGetString(s, "trackId") ?? TryGetString(s, "trackRef"),
                BodyPointZoneRef = TryGetString(s, "bodyPointZoneId") ?? TryGetString(s, "bodyPointZoneRef"),
                NearestShapeRef = TryGetString(s, "shapeId") ?? TryGetString(s, "nearestShapeRef"),
                BodyPointId = TryGetInt(s, "bodyPointId"),
                RawX = TryGetNumber(s, "rawWorldX") ?? TryGetNumber(s, "rawX"),
                RawY = TryGetNumber(s, "rawWorldY") ?? TryGetNumber(s, "rawY"),
                CorrectedX = TryGetNumber(s, "correctedWorldX") ?? TryGetNumber(s, "worldX") ?? TryGetNumber(s, "correctedX"),
                CorrectedY = TryGetNumber(s, "correctedWorldY") ?? TryGetNumber(s, "worldY") ?? TryGetNumber(s, "correctedY"),
                Xmm = TryGetNumber(s, "xMm") ?? TryGetNumber(s, "cursor_x_mm") ?? TryGetNumber(s, "pacer_x_mm"),
                Ymm = TryGetNumber(s, "yMm") ?? TryGetNumber(s, "cursor_y_mm") ?? TryGetNumber(s, "pacer_y_mm"),
                Speed = TryGetNumber(s, "speedMmS") ?? TryGetNumber(s, "velocity_total_mm_s"),
                InsideCorridor = TryGetBool(s, "insideCorridor") ?? TryGetBool(s, "inside_corridor"),
                DistanceToTrackCenterMm = TryGetNumber(s, "deviationMm") ?? TryGetNumber(s, "distance_to_track_center_mm"),
                DistanceToPacerMm = TryGetNumber(s, "distanceToPacerMm") ?? TryGetNumber(s, "distance_to_pacer_mm"),
                SignalStatus = MapSignalStatus(TryGetString(s, "signalStatus")),
                Confidence = TryGetNumber(s, "confidence"),
                ExtraJson = s.GetRawText()
            };

            _db.SessionSamples.Add(sample);
            i++;
        }
    }

    private static SessionEvent DemoEvent(int sessionId, SessionEventType type, decimal t, string payload) => new()
    {
        SessionId = sessionId,
        Type = type,
        T = t,
        Payload = payload,
        CreatedAt = DateTime.UtcNow
    };

    private static SessionEventType MapEventType(string code)
    {
        var c = code.Trim().ToLowerInvariant();
        return c switch
        {
            "session_started" or "exercise_started" => SessionEventType.SessionStarted,
            "pacer_started" => SessionEventType.PacerStarted,
            "corridor_exit" or "cursor_left_corridor" => SessionEventType.CorridorExit,
            "target_hit" => SessionEventType.TargetHit,
            "target_missed" => SessionEventType.TargetMissed,
            "pacer_lag" => SessionEventType.PacerLag,
            "pacer_lead" => SessionEventType.PacerLead,
            "signal_lost" => SessionEventType.SignalLost,
            "signal_restored" => SessionEventType.SignalRestored,
            "session_completed" or "exercise_finished" => SessionEventType.SessionCompleted,
            _ => SessionEventType.OperatorComment
        };
    }

    private static SignalStatus MapSignalStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return SignalStatus.Ok;
        return status.Trim().ToLowerInvariant() switch
        {
            "lost" or "signal_lost" => SignalStatus.Lost,
            "low" or "lowquality" or "low_quality" => SignalStatus.LowQuality,
            "interpolated" => SignalStatus.Interpolated,
            _ => SignalStatus.Ok
        };
    }

    private static string BuildSummary(JsonElement root)
    {
        var mean = TryGetNumberRecursive(root, "meanDeviationMm");
        var rmse = TryGetNumberRecursive(root, "rmseDeviationMm");
        var outside = TryGetNumberRecursive(root, "timeOutsideSec");
        return $"Imported Unity session. meanDeviationMm={mean?.ToString() ?? "n/a"}, rmseDeviationMm={rmse?.ToString() ?? "n/a"}, timeOutsideSec={outside?.ToString() ?? "n/a"}.";
    }

    private static JsonElement? TryGetArray(JsonElement root, string name)
    {
        if (root.ValueKind != JsonValueKind.Object) return null;
        foreach (var p in root.EnumerateObject())
        {
            if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase) && p.Value.ValueKind == JsonValueKind.Array)
                return p.Value;
        }
        return null;
    }

    private static string? TryGetObjectRaw(JsonElement root, string name)
    {
        if (root.ValueKind != JsonValueKind.Object) return null;
        foreach (var p in root.EnumerateObject())
        {
            if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase) && p.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                return p.Value.GetRawText();
        }
        return null;
    }

    private static decimal? TryGetNumberRecursive(JsonElement root, string name)
    {
        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var p in root.EnumerateObject())
            {
                if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase) && p.Value.ValueKind == JsonValueKind.Number)
                    return p.Value.GetDecimal();

                var nested = TryGetNumberRecursive(p.Value, name);
                if (nested.HasValue) return nested;
            }
        }
        else if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
            {
                var nested = TryGetNumberRecursive(item, name);
                if (nested.HasValue) return nested;
            }
        }
        return null;
    }

    private static decimal? TryGetNumber(JsonElement root, string name)
    {
        if (root.ValueKind != JsonValueKind.Object) return null;
        foreach (var p in root.EnumerateObject())
            if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase) && p.Value.ValueKind == JsonValueKind.Number)
                return p.Value.GetDecimal();
        return null;
    }

    private static int? TryGetInt(JsonElement root, string name)
    {
        var value = TryGetNumber(root, name);
        return value.HasValue ? (int)value.Value : null;
    }

    private static bool? TryGetBool(JsonElement root, string name)
    {
        if (root.ValueKind != JsonValueKind.Object) return null;
        foreach (var p in root.EnumerateObject())
            if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase) && p.Value.ValueKind is JsonValueKind.True or JsonValueKind.False)
                return p.Value.GetBoolean();
        return null;
    }

    private static string? TryGetString(JsonElement root, string name)
    {
        if (root.ValueKind != JsonValueKind.Object) return null;
        foreach (var p in root.EnumerateObject())
            if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase) && p.Value.ValueKind == JsonValueKind.String)
                return p.Value.GetString();
        return null;
    }
}
