using ChefExpress.Application.Abstractions.Authentication;
using ChefExpress.Application.Abstractions.Persistence;
using ChefExpress.Application.Common;
using ChefExpress.Domain.Users;

namespace ChefExpress.Application.Auth;

internal sealed class AuthService(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ITokenService tokenService) : IAuthService
{
    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var errors = AuthValidator.Validate(request);
        if (errors.Count > 0)
            return Error.Validation(errors);

        var email = User.NormalizeEmail(request.Email!);
        if (await users.EmailExistsAsync(email, cancellationToken))
            return AuthErrors.EmailAlreadyInUse;

        var user = User.Create(request.Name!, email, passwordHasher.Hash(request.Password!), request.Role!.Value);
        users.Add(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return SignIn(user);
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var errors = AuthValidator.Validate(request);
        if (errors.Count > 0)
            return Error.Validation(errors);

        var user = await users.GetByEmailAsync(User.NormalizeEmail(request.Email!), cancellationToken);
        if (user is null)
            return AuthErrors.InvalidCredentials;

        switch (passwordHasher.Verify(user.PasswordHash, request.Password!))
        {
            case PasswordCheck.Failed:
                return AuthErrors.InvalidCredentials;
            case PasswordCheck.SuccessRehashNeeded:
                user.ChangePasswordHash(passwordHasher.Hash(request.Password!));
                await unitOfWork.SaveChangesAsync(cancellationToken);
                break;
        }

        return SignIn(user);
    }

    public async Task<Result<UserResponse>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken);
        return user is null ? AuthErrors.UserNotFound : UserResponse.From(user);
    }

    private AuthResponse SignIn(User user)
    {
        var token = tokenService.CreateAccessToken(user);
        return new AuthResponse(token.Value, token.ExpiresAt, UserResponse.From(user));
    }
}
