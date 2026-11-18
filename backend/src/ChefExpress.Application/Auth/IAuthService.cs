using ChefExpress.Application.Common;

namespace ChefExpress.Application.Auth;

public interface IAuthService
{
    /// <summary>Creates the account (and an empty profile for professionals) and signs the user in.</summary>
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<Result<UserResponse>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
