using ChefExpress.Application.Abstractions.Authentication;
using ChefExpress.Domain.Users;
using ChefExpress.Infrastructure.Authentication;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ChefExpress.UnitTests.Auth;

public sealed class AuthInfrastructureTests
{
    private static readonly JwtOptions Options = new()
    {
        Issuer = "test-issuer",
        Audience = "test-audience",
        Key = "unit-test-key-that-is-long-enough-for-hs256!!",
        ExpirationMinutes = 60
    };

    [Fact]
    public void Password_hash_is_salted_and_verifies()
    {
        var hasher = new PasswordHasher();

        var first = hasher.Hash("senhaSegura1");
        var second = hasher.Hash("senhaSegura1");

        Assert.NotEqual(first, second);
        Assert.DoesNotContain("senhaSegura1", first);
        Assert.Equal(PasswordCheck.Success, hasher.Verify(first, "senhaSegura1"));
        Assert.Equal(PasswordCheck.Failed, hasher.Verify(first, "senhaErrada1"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-base64-%%%")]
    public void Password_verify_fails_on_invalid_hash(string storedHash) =>
        Assert.Equal(PasswordCheck.Failed, new PasswordHasher().Verify(storedHash, "senhaSegura1"));

    [Fact]
    public async Task Jwt_contains_user_claims_and_validates_with_same_key()
    {
        var user = User.Create("Maria", "maria@example.com", "hash", UserRole.Professional);
        var service = new JwtTokenService(Options, TimeProvider.System);

        var token = service.CreateAccessToken(user);

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token.Value, new TokenValidationParameters
        {
            ValidIssuer = Options.Issuer,
            ValidAudience = Options.Audience,
            IssuerSigningKey = Options.GetSigningKey()
        });

        Assert.True(result.IsValid, result.Exception?.Message);
        Assert.Equal(user.Id.ToString(), result.Claims[AuthClaims.UserId]);
        Assert.Equal("Professional", result.Claims[AuthClaims.Role]);
        Assert.Equal("maria@example.com", result.Claims[JwtRegisteredClaimNames.Email]);
        Assert.InRange(token.ExpiresAt, DateTime.UtcNow.AddMinutes(59), DateTime.UtcNow.AddMinutes(61));
    }

    [Fact]
    public async Task Jwt_signed_with_another_key_is_rejected()
    {
        var user = User.Create("Maria", "maria@example.com", "hash", UserRole.Client);
        var token = new JwtTokenService(Options, TimeProvider.System).CreateAccessToken(user);

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token.Value, new TokenValidationParameters
        {
            ValidIssuer = Options.Issuer,
            ValidAudience = Options.Audience,
            IssuerSigningKey = new JwtOptions { Key = "a-completely-different-key-with-32-bytes+" }.GetSigningKey()
        });

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("short-key")]
    public void Jwt_options_reject_weak_keys(string key) =>
        Assert.Throws<InvalidOperationException>(() => new JwtOptions { Key = key }.EnsureValid());
}
