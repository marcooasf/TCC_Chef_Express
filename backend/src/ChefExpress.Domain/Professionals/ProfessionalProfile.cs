using ChefExpress.Domain.Common;
using ChefExpress.Domain.Geography;
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

    /// <summary>
    /// Approximate location used for proximity search: the centre of the neighbourhood or city
    /// returned by OpenStreetMap, never a street address (LGPD). Null while not geocoded, or when
    /// the lookup failed; the search then falls back to the city/state filter.
    /// </summary>
    public double? Latitude { get; private set; }

    /// <inheritdoc cref="Latitude"/>
    public double? Longitude { get; private set; }

    /// <summary>Stored coordinates are rounded to 3 decimals (about 110 m).</summary>
    public const int LocationDecimals = 3;

    public GeoPoint? Location =>
        Latitude is { } lat && Longitude is { } lon ? new GeoPoint(lat, lon) : null;

    public bool HasAddress => !string.IsNullOrWhiteSpace(City) && BrazilianStates.IsValid(State);

    public ICollection<Skill> Skills { get; private set; } = [];
    public ICollection<AvailableDate> AvailableDates { get; private set; } = [];

    private ProfessionalProfile() { } // EF Core

    public static ProfessionalProfile CreateFor(User user) =>
        new() { UserId = user.Id, User = user, UpdatedAt = DateTime.UtcNow };

    /// <summary>
    /// Sets city, UF and optional neighbourhood. If the address really changed (case-insensitive),
    /// the stored location is cleared so it is never stale; geocode again afterwards
    /// (<c>IProfileLocationService</c>). Returns whether the address changed.
    /// </summary>
    public bool UpdateAddress(string city, string state, string? neighborhood)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(city);
        if (!BrazilianStates.IsValid(state))
            throw new ArgumentException($"'{state}' is not a valid Brazilian state (UF).", nameof(state));

        city = city.Trim();
        state = state.Trim().ToUpperInvariant();
        neighborhood = string.IsNullOrWhiteSpace(neighborhood) ? null : neighborhood.Trim();

        var changed =
            !string.Equals(City, city, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(State, state, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(Neighborhood, neighborhood, StringComparison.OrdinalIgnoreCase);

        City = city;
        State = state;
        Neighborhood = neighborhood;

        if (changed)
            ClearLocation();
        else
            Touch();

        return changed;
    }

    public void SetLocation(GeoPoint point)
    {
        var rounded = point.Round(LocationDecimals);
        Latitude = rounded.Latitude;
        Longitude = rounded.Longitude;
        Touch();
    }

    public void ClearLocation()
    {
        Latitude = null;
        Longitude = null;
        Touch();
    }

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
