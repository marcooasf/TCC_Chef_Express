using ChefExpress.Domain.Users;

namespace ChefExpress.Application.Abstractions.Authentication;

public sealed record AccessToken(string Value, DateTime ExpiresAt);

public interface ITokenService
{
    AccessToken CreateAccessToken(User user);
}
