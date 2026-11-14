using ChefExpress.Domain.Common;

namespace ChefExpress.Domain.Professionals;

/// <summary>
/// A whole day (no time-of-day, see "Risco 2" in the planning doc) on which the professional is free.
/// <see cref="IsBooked"/> becomes true once a simulated payment confirms an order for that day.
/// </summary>
public sealed class AvailableDate : Entity
{
    public Guid ProfessionalProfileId { get; private set; }
    public DateOnly Date { get; private set; }
    public bool IsBooked { get; private set; }

    private AvailableDate() { } // EF Core

    internal AvailableDate(Guid professionalProfileId, DateOnly date)
    {
        ProfessionalProfileId = professionalProfileId;
        Date = date;
    }

    public void Book()
    {
        if (IsBooked)
            throw new InvalidOperationException($"Date {Date} is already booked.");
        IsBooked = true;
    }
}
