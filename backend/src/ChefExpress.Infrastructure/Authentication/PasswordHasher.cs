using ChefExpress.Application.Abstractions.Authentication;
using Microsoft.AspNetCore.Identity;

namespace ChefExpress.Infrastructure.Authentication;

/// <summary>
/// Uses ASP.NET Core Identity's hasher (PBKDF2-HMAC-SHA512, 100k iterations, random salt, versioned format)
/// without pulling in the rest of Identity.
/// </summary>
internal sealed class PasswordHasher : IPasswordHasher
{
    private static readonly object User = new();
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(User, password);

    public PasswordCheck Verify(string passwordHash, string password)
    {
        try
        {
            return _hasher.VerifyHashedPassword(User, passwordHash, password) switch
            {
                PasswordVerificationResult.Success => PasswordCheck.Success,
                PasswordVerificationResult.SuccessRehashNeeded => PasswordCheck.SuccessRehashNeeded,
                _ => PasswordCheck.Failed
            };
        }
        catch (FormatException)
        {
            // Corrupted/non-Base64 hash in the database: treat as a wrong password, not a 500.
            return PasswordCheck.Failed;
        }
    }
}
