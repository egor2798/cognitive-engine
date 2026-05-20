namespace CognitiveEngine.API.Dtos;

public sealed record LoginRequestDto(
    string? Username,
    string? Login,
    string? LoginOrEmail,
    string? Email,
    string Password
);

public sealed record RegisterRequestDto(
    string? Username,
    string? Login,
    string? LoginOrEmail,
    string Email,
    string Password,
    string? Role,
    string? FirstName,
    string? LastName
);

public sealed record AuthUserDto(
    int Id,
    string Username,
    string Email,
    string Role,
    string Name,
    int OrganizationId
);

public sealed record AuthResponseDto(
    string Token,
    AuthUserDto User
);
