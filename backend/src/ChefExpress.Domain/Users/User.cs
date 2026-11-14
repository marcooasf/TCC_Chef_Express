using ChefExpress.Domain.Common;
using ChefExpress.Domain.Professionals;

namespace ChefExpress.Domain.Users;

/// <summary>
/// Platform user. Credentials and the Client/Professional role are added by the
/// authentication task (task 4) in its own migration.
/// </summary>
public sealed class User : Entity
{
    public const int NameMaxLength = 120;
    public const int EmailMaxLength = 254;

    public string Name { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    /// <summary>Only exists for professionals.</summary>
    public ProfessionalProfile? ProfessionalProfile { get; private set; }

    private User() { } // EF Core

    public static User Create(string name, string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        return new User
        {
            Name = name.Trim(),
            Email = NormalizeEmail(email),
            CreatedAt = DateTime.UtcNow
        };
    }

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
