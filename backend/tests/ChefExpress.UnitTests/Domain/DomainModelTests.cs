using ChefExpress.Domain.Professionals;
using ChefExpress.Domain.Users;

namespace ChefExpress.UnitTests.Domain;

public sealed class DomainModelTests
{
    [Fact]
    public void User_create_trims_name_and_normalizes_email()
    {
        var user = User.Create("  Maria  ", "  Maria@Example.COM ");

        Assert.Equal("Maria", user.Name);
        Assert.Equal("maria@example.com", user.Email);
        Assert.NotEqual(Guid.Empty, user.Id);
    }

    [Theory]
    [InlineData("", "maria@example.com")]
    [InlineData("Maria", " ")]
    public void User_create_rejects_blank_fields(string name, string email) =>
        Assert.ThrowsAny<ArgumentException>(() => User.Create(name, email));

    [Fact]
    public void Adding_the_same_available_date_twice_keeps_a_single_entry()
    {
        var profile = ProfessionalProfile.CreateFor(User.Create("João", "joao@example.com"));
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
        var profile = ProfessionalProfile.CreateFor(User.Create("João", "joao@example.com"));
        var date = profile.AddAvailableDate(new DateOnly(2026, 12, 20));

        date.Book();

        Assert.True(date.IsBooked);
        Assert.Throws<InvalidOperationException>(date.Book);
    }
}
