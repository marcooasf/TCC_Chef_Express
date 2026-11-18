using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ChefExpress.Infrastructure.Authentication;

/// <summary>JWT settings ("Jwt" section). In production, set <c>Jwt__Key</c> via environment variable.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public const int MinKeyBytes = 32; // HMAC-SHA256 needs at least 256 bits

    public string Issuer { get; init; } = "ChefExpress";
    public string Audience { get; init; } = "ChefExpress";
    /// <summary>Secret used to sign tokens (HS256). At least 32 bytes.</summary>
    public string Key { get; init; } = string.Empty;
    public int ExpirationMinutes { get; init; } = 120;

    public SymmetricSecurityKey GetSigningKey() => new(Encoding.UTF8.GetBytes(Key));

    /// <summary>Fails fast at startup instead of issuing unverifiable/weak tokens.</summary>
    public void EnsureValid()
    {
        if (Encoding.UTF8.GetByteCount(Key) < MinKeyBytes)
            throw new InvalidOperationException(
                $"'{SectionName}:Key' must be configured with at least {MinKeyBytes} bytes (env var Jwt__Key).");
        if (ExpirationMinutes <= 0)
            throw new InvalidOperationException($"'{SectionName}:ExpirationMinutes' must be positive.");
    }
}
