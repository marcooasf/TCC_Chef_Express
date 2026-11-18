using ChefExpress.Domain.Common;
using ChefExpress.Domain.Professionals;

namespace ChefExpress.Domain.Users;

/// <summary>Platform user: a Client (hires) or a Professional (offers services).</summary>
public sealed class User : Entity
{
    public const int NameMaxLength = 120;
    public const int EmailMaxLength = 254;

    public string Name { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    /// <summary>Hashed password (never the plain text). The hashing algorithm lives in Infrastructure.</summary>
    public string PasswordHash { get; private set; } = null!;
    public UserRole Role { get; private set; }
    public DateTime CreatedAt { get; private set; }

    /// <summary>Only exists for professionals.</summary>
    public ProfessionalProfile? ProfessionalProfile { get; private set; }

    private User() { } // EF Core

    /// <summary>
    /// Creates a user. Professionals get an empty <see cref="ProfessionalProfile"/>
    /// right away, to be filled in on the "Meu Perfil e Agenda" screen.
    /// </summary>
    public static User Create(string name, string email, string passwordHash, UserRole role)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        if (!Enum.IsDefined(role))
            throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown user role.");

        var user = new User
        {
            Name = name.Trim(),
            Email = NormalizeEmail(email),
            PasswordHash = passwordHash,
            Role = role,
            CreatedAt = DateTime.UtcNow
        };

        if (role == UserRole.Professional)
            user.ProfessionalProfile = ProfessionalProfile.CreateFor(user);

        return user;
    }

    public void ChangePasswordHash(string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        PasswordHash = passwordHash;
    }

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
