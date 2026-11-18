using ChefExpress.Domain.Professionals;
using ChefExpress.Domain.Users;

namespace ChefExpress.UnitTests.Domain;

public sealed class DomainModelTests
{
    private const string Hash = "hashed-password";

    [Fact]
    public void Professional_sign_up_creates_an_empty_profile()
    {
        var user = User.Create("João", "joao@example.com", Hash, UserRole.Professional);

        Assert.NotNull(user.ProfessionalProfile);
        Assert.Equal(user.Id, user.ProfessionalProfile.UserId);
    }

    [Fact]
    public void Client_sign_up_has_no_professional_profile() =>
        Assert.Null(User.Create("Maria", "maria@example.com", Hash, UserRole.Client).ProfessionalProfile);

    [Fact]
    public void User_create_rejects_unknown_role() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => User.Create("Maria", "maria@example.com", Hash, (UserRole)99));

    [Fact]
    public void User_create_trims_name_and_normalizes_email()
    {
        var user = User.Create("  Maria  ", "  Maria@Example.COM ", Hash, UserRole.Client);

        Assert.Equal("Maria", user.Name);
        Assert.Equal("maria@example.com", user.Email);
        Assert.NotEqual(Guid.Empty, user.Id);
    }

    [Theory]
    [InlineData("", "maria@example.com")]
    [InlineData("Maria", " ")]
    public void User_create_rejects_blank_fields(string name, string email) =>
        Assert.ThrowsAny<ArgumentException>(() => User.Create(name, email, Hash, UserRole.Client));

    [Fact]
    public void Adding_the_same_available_date_twice_keeps_a_single_entry()
    {
        var profile = ProfessionalProfile.CreateFor(User.Create("João", "joao@example.com", Hash, UserRole.Professional));
        var day = new DateOnly(2026, 12, 20);

        var first = profile.AddAvailableDate(day);
        var second = profile.AddAvailableDate(day);

        Assert.Same(first, second);
        Assert.Single(profile.AvailableDates);
        Assert.Equal(profile.Id, first.ProfessionalProfileId);
    }

    [Fact]
    public void Booking_a_date_twice_fails()
    {
        var profile = ProfessionalProfile.CreateFor(User.Create("João", "joao@example.com", Hash, UserRole.Professional));
        var date = profile.AddAvailableDate(new DateOnly(2026, 12, 20));

        date.Book();

        Assert.True(date.IsBooked);
        Assert.Throws<InvalidOperationException>(date.Book);
    }
}
