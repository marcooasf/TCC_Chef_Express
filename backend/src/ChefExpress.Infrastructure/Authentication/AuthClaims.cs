namespace ChefExpress.Infrastructure.Authentication;

/// <summary>Claim names written into the JWT and read back by the API.</summary>
public static class AuthClaims
{
    public const string UserId = "sub";
    public const string Role = "role";
}
