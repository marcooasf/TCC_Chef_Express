using ChefExpress.Domain.Common;
using ChefExpress.Domain.Users;

namespace ChefExpress.Domain.Professionals;

/// <summary>
/// Public profile of a professional (story 1). Profile editing and agenda endpoints
/// are part of Module 1 (tasks 6–8); this week only the table structure is created.
/// When the profile is created (e.g. on Professional sign-up) is up to the auth task.
/// </summary>
public sealed class ProfessionalProfile : Entity
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    public string? Description { get; private set; }
    public string City { get; private set; } = string.Empty;
    public string State { get; private set; } = string.Empty;
    /// <summary>Optional — the location filter granularity is still "[A VERIFICAR]".</summary>
    public string? Neighborhood { get; private set; }
    public decimal BasePrice { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public ICollection<Skill> Skills { get; private set; } = [];
    public ICollection<AvailableDate> AvailableDates { get; private set; } = [];

    private ProfessionalProfile() { } // EF Core

    public static ProfessionalProfile CreateFor(User user) =>
        new() { UserId = user.Id, User = user, UpdatedAt = DateTime.UtcNow };

    public AvailableDate AddAvailableDate(DateOnly date)
    {
        var existing = AvailableDates.FirstOrDefault(d => d.Date == date);
        if (existing is not null)
            return existing;

        var available = new AvailableDate(Id, date);
        AvailableDates.Add(available);
        Touch();
        return available;
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}
