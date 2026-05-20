using System.Text;
using CognitiveEngine.API.Dtos;
using CognitiveEngine.API.Models;
using CognitiveEngine.API.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CognitiveEngine.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AppDbContext _db;

    public AuthController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto request, CancellationToken ct)
    {
        var loginOrEmail = FirstNotEmpty(
            request.Username,
            request.Login,
            request.LoginOrEmail,
            request.Email
        )?.Trim();

        if (string.IsNullOrWhiteSpace(loginOrEmail) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Введите логин и пароль." });

        var normalized = loginOrEmail.ToLowerInvariant();

        var user = await _db.Users
            .AsTracking()
            .FirstOrDefaultAsync(x =>
                x.IsActive &&
                (
                    x.Login.ToLower() == normalized ||
                    x.Email.ToLower() == normalized
                ),
                ct
            );

        if (user == null)
            return Unauthorized(new { message = "Неверный логин или пароль" });

        // Демо-вход для локальной версии проекта.
        // Это специально сделано, чтобы не зависеть от старых seed-хэшей в БД.
        var demoPasswordOk =
            (request.Password == "123456" || request.Password == "Demo@12345") &&
            IsDemoUser(user.Login);

        // Если в БД внезапно лежит обычный текстовый пароль, тоже разрешим.
        var plainPasswordOk =
            !string.IsNullOrWhiteSpace(user.PasswordHash) &&
            user.PasswordHash == request.Password;

        if (!demoPasswordOk && !plainPasswordOk)
        {
            user.FailedLoginAttempts += 1;
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return Unauthorized(new { message = "Неверный логин или пароль" });
        }

        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Ok(new AuthResponseDto(
            Token: CreateDemoToken(user),
            User: ToDto(user)
        ));
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterRequestDto request, CancellationToken ct)
    {
        var login = FirstNotEmpty(request.Username, request.Login, request.LoginOrEmail)?.Trim();
        var email = request.Email?.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(login) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Введите логин, email и пароль." });
        }

        if (login.Length < 3)
            return BadRequest(new { message = "Логин должен быть не короче 3 символов." });

        if (request.Password.Length < 6)
            return BadRequest(new { message = "Пароль должен быть не короче 6 символов." });

        var normalizedLogin = login.ToLowerInvariant();

        var exists = await _db.Users.AnyAsync(x =>
            x.Login.ToLower() == normalizedLogin ||
            x.Email.ToLower() == email,
            ct
        );

        if (exists)
            return Conflict(new { message = "Пользователь с таким логином или email уже существует." });

        var organizationId = await EnsureDefaultOrganization(ct);

        var user = new User
        {
            Login = login,
            Email = email,
            FirstName = request.FirstName?.Trim() ?? "",
            LastName = request.LastName?.Trim() ?? "",
            PasswordHash = request.Password, // для локальной демо-версии
            Role = ParseRole(request.Role),
            OrganizationId = organizationId,
            IsActive = true,
            FailedLoginAttempts = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return Ok(new AuthResponseDto(
            Token: CreateDemoToken(user),
            User: ToDto(user)
        ));
    }

    [HttpGet("me")]
    public async Task<ActionResult<AuthUserDto>> Me(CancellationToken ct)
    {
        var auth = Request.Headers.Authorization.ToString();
        var userId = TryReadUserIdFromDemoToken(auth);

        if (userId == null)
            return Unauthorized(new { message = "Нет авторизации." });

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId.Value && x.IsActive, ct);

        if (user == null)
            return Unauthorized(new { message = "Пользователь не найден." });

        return Ok(ToDto(user));
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
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Organizations.Add(org);
        await _db.SaveChangesAsync(ct);
        return org.Id;
    }

    private static bool IsDemoUser(string login)
    {
        var value = (login ?? "").Trim().ToLowerInvariant();
        return value is "admin" or "operator" or "methodist" or "researcher";
    }

    private static string? FirstNotEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }

    private static UserRole ParseRole(string? role)
    {
        var value = (role ?? "operator").Trim().ToLowerInvariant();

        return value switch
        {
            "admin" => UserRole.Admin,
            "methodist" => UserRole.Methodist,
            "researcher" => UserRole.Researcher,
            "patient" => UserRole.Patient,
            "specialist" => UserRole.Operator,
            "operator" => UserRole.Operator,
            _ => UserRole.Operator
        };
    }

    private static AuthUserDto ToDto(User user)
    {
        var roleName = user.Role switch
        {
            UserRole.Admin => "admin",
            UserRole.Methodist => "methodist",
            UserRole.Researcher => "researcher",
            UserRole.Patient => "patient",
            _ => "specialist"
        };

        var name = string.Join(" ", new[]
        {
            user.LastName,
            user.FirstName,
            user.MiddleName
        }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();

        if (string.IsNullOrWhiteSpace(name))
            name = user.Login;

        return new AuthUserDto(
            Id: user.Id,
            Username: user.Login,
            Email: user.Email,
            Role: roleName,
            Name: name,
            OrganizationId: user.OrganizationId
        );
    }

    private static string CreateDemoToken(User user)
    {
        var payload = $"{user.Id}|{user.Login}|{user.Role}|{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
    }

    private static int? TryReadUserIdFromDemoToken(string authHeader)
    {
        if (string.IsNullOrWhiteSpace(authHeader))
            return null;

        var token = authHeader.Replace("Bearer", "", StringComparison.OrdinalIgnoreCase).Trim();

        try
        {
            var payload = Encoding.UTF8.GetString(Convert.FromBase64String(token));
            var idText = payload.Split('|').FirstOrDefault();

            return int.TryParse(idText, out var id)
                ? id
                : null;
        }
        catch
        {
            return null;
        }
    }
}
