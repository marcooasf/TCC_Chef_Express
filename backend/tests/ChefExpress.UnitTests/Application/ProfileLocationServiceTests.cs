using ChefExpress.Application.Abstractions.Geocoding;
using ChefExpress.Application.Common;
using ChefExpress.Application.Professionals;
using ChefExpress.Domain.Geography;
using ChefExpress.Domain.Professionals;
using ChefExpress.Domain.Users;

namespace ChefExpress.UnitTests.Application;

public sealed class ProfileLocationServiceTests
{
    [Fact]
    public async Task Successful_lookup_stores_the_rounded_location()
    {
        var geocoder = new FakeGeocoder(new GeocodedPlace(new GeoPoint(-26.91849, -49.09251), GeocodingPrecision.Neighborhood, "Velha"));
        var profile = ProfileIn("Blumenau", "SC", "Velha");

        var result = await new ProfileLocationService(geocoder).RefreshLocationAsync(profile);

        Assert.True(result.IsSuccess);
        Assert.Equal(new GeoPoint(-26.918, -49.093), profile.Location);
        Assert.Equal(new AddressQuery("Blumenau", "SC", "Velha"), geocoder.LastQuery);
    }

    [Fact]
    public async Task Failed_lookup_clears_the_location_and_returns_the_error()
    {
        var profile = ProfileIn("Blumenau", "SC", null);
        profile.SetLocation(new GeoPoint(-26.9, -49.1));

        var result = await new ProfileLocationService(new FakeGeocoder(GeocodingErrors.Unavailable)).RefreshLocationAsync(profile);

        Assert.False(result.IsSuccess);
        Assert.Equal(GeocodingErrors.Unavailable, result.Error);
        Assert.Null(profile.Location);
    }

    [Fact]
    public async Task Profile_without_address_is_not_sent_to_the_geocoder()
    {
        var geocoder = new FakeGeocoder(GeocodingErrors.NotFound);
        var profile = ProfessionalProfile.CreateFor(User.Create("Ana", "ana@example.com"));

        var result = await new ProfileLocationService(geocoder).RefreshLocationAsync(profile);

        Assert.Equal(GeocodingErrors.MissingAddress, result.Error);
        Assert.Null(geocoder.LastQuery);
    }

    private static ProfessionalProfile ProfileIn(string city, string state, string? neighborhood)
    {
        var profile = ProfessionalProfile.CreateFor(User.Create("Ana", "ana@example.com"));
        profile.UpdateAddress(city, state, neighborhood);
        return profile;
    }

    private sealed class FakeGeocoder(Result<GeocodedPlace> result) : IGeocodingService
    {
        public FakeGeocoder(GeocodedPlace place) : this((Result<GeocodedPlace>)place) { }
        public FakeGeocoder(Error error) : this((Result<GeocodedPlace>)error) { }

        public AddressQuery? LastQuery { get; private set; }

        public Task<Result<GeocodedPlace>> GeocodeAsync(AddressQuery address, CancellationToken cancellationToken = default)
        {
            LastQuery = address;
            return Task.FromResult(result);
        }
    }
}
