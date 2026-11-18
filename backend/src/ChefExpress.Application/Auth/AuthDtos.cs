using ChefExpress.Domain.Users;

namespace ChefExpress.Application.Auth;

// Fields are nullable so missing JSON fields reach our validator (pt-BR messages)
// instead of failing model binding with framework messages.
public sealed record RegisterRequest(string? Name, string? Email, string? Password, UserRole? Role);

public sealed record LoginRequest(string? Email, string? Password);

public sealed record UserResponse(Guid Id, string Name, string Email, UserRole Role, DateTime CreatedAt)
{
    public static UserResponse From(User user) => new(user.Id, user.Name, user.Email, user.Role, user.CreatedAt);
}

/// <param name="AccessToken">JWT to send as <c>Authorization: Bearer &lt;token&gt;</c>.</param>
public sealed record AuthResponse(string AccessToken, DateTime ExpiresAt, UserResponse User);
