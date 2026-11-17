using ChefExpress.Domain.Geography;
using ChefExpress.Domain.Professionals;
using ChefExpress.Domain.Users;

namespace ChefExpress.UnitTests.Domain;

public sealed class GeographyTests
{
    private static readonly GeoPoint Blumenau = new(-26.9195567, -49.0658025);
    private static readonly GeoPoint Florianopolis = new(-27.5954, -48.5480);

    [Theory]
    [InlineData(91, 0)]
    [InlineData(-91, 0)]
    [InlineData(0, 181)]
    [InlineData(0, -181)]
    [InlineData(double.NaN, 0)]
    [InlineData(0, double.NaN)]
    public void GeoPoint_rejects_out_of_range_coordinates(double lat, double lon) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new GeoPoint(lat, lon));

    [Fact]
    public void One_degree_of_longitude_on_the_equator_is_about_111_km()
    {
        var distance = new GeoPoint(0, 0).DistanceKmTo(new GeoPoint(0, 1));

        Assert.Equal(111.195, distance, precision: 3);
    }

    [Fact]
    public void Distance_is_symmetric_and_zero_to_itself()
    {
        Assert.Equal(0, Blumenau.DistanceKmTo(Blumenau), precision: 9);
        Assert.Equal(Blumenau.DistanceKmTo(Florianopolis), Florianopolis.DistanceKmTo(Blumenau), precision: 9);
        Assert.InRange(Blumenau.DistanceKmTo(Florianopolis), 85, 95); // ~88 km in a straight line
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Bounding_box_requires_a_positive_radius(double radius) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => GeoBoundingBox.Around(Blumenau, radius));

    [Theory]
    [InlineData(1)]
    [InlineData(25)]
    [InlineData(300)]
    public void Bounding_box_contains_every_point_on_the_circle(double radiusKm)
    {
        var box = GeoBoundingBox.Around(Blumenau, radiusKm);

        // Walk the circle (slightly inside it, to avoid rounding at the border)
        for (var bearing = 0; bearing < 360; bearing += 5)
        {
            var point = Destination(Blumenau, radiusKm * 0.999, bearing);
            Assert.True(box.Contains(point), $"bearing {bearing}° is outside the box");
        }

        Assert.False(box.Contains(Destination(Blumenau, radiusKm * 1.5, 0)));
    }

    [Fact]
    public void Bounding_box_near_the_pole_covers_all_longitudes()
    {
        var box = GeoBoundingBox.Around(new GeoPoint(89.9, 10), 50);

        Assert.Equal(-180, box.MinLongitude);
        Assert.Equal(180, box.MaxLongitude);
        Assert.Equal(90, box.MaxLatitude);
    }

    [Theory]
    [InlineData("sc", true)]
    [InlineData(" SP ", true)]
    [InlineData("XX", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Brazilian_state_codes_are_validated(string? code, bool valid) =>
        Assert.Equal(valid, BrazilianStates.IsValid(code));

    [Fact]
    public void Update_address_normalizes_and_clears_a_stale_location()
    {
        var profile = NewProfile();
        profile.UpdateAddress("Blumenau", "SC", "Velha");
        profile.SetLocation(Blumenau);

        var changed = profile.UpdateAddress("  Blumenau ", "sc", "Garcia");

        Assert.True(changed);
        Assert.Equal("SC", profile.State);
        Assert.Equal("Blumenau", profile.City);
        Assert.Equal("Garcia", profile.Neighborhood);
        Assert.Null(profile.Location);
    }

    [Fact]
    public void Update_address_with_only_case_changes_keeps_the_location()
    {
        var profile = NewProfile();
        profile.UpdateAddress("Blumenau", "SC", "Velha");
        profile.SetLocation(Blumenau);

        var changed = profile.UpdateAddress("BLUMENAU", "sc", "velha");

        Assert.False(changed);
        Assert.NotNull(profile.Location);
    }

    [Fact]
    public void Update_address_rejects_an_unknown_state() =>
        Assert.Throws<ArgumentException>(() => NewProfile().UpdateAddress("Blumenau", "XX", null));

    [Fact]
    public void Blank_neighborhood_is_stored_as_null()
    {
        var profile = NewProfile();
        profile.UpdateAddress("Blumenau", "SC", "   ");

        Assert.Null(profile.Neighborhood);
        Assert.True(profile.HasAddress);
    }

    [Fact]
    public void Stored_location_is_rounded_to_about_100_meters()
    {
        var profile = NewProfile();

        profile.SetLocation(new GeoPoint(-26.9195567, -49.0658025));

        Assert.Equal(-26.920, profile.Latitude);
        Assert.Equal(-49.066, profile.Longitude);
    }

    private static ProfessionalProfile NewProfile() =>
        ProfessionalProfile.CreateFor(User.Create("João", "joao@example.com"));

    /// <summary>Point at <paramref name="distanceKm"/> from <paramref name="origin"/> along a bearing.</summary>
    private static GeoPoint Destination(GeoPoint origin, double distanceKm, double bearingDegrees)
    {
        const double rad = Math.PI / 180;
        var angular = distanceKm / GeoPoint.EarthRadiusKm;
        var bearing = bearingDegrees * rad;
        var lat1 = origin.Latitude * rad;
        var lon1 = origin.Longitude * rad;

        var lat2 = Math.Asin(Math.Sin(lat1) * Math.Cos(angular) + Math.Cos(lat1) * Math.Sin(angular) * Math.Cos(bearing));
        var lon2 = lon1 + Math.Atan2(
            Math.Sin(bearing) * Math.Sin(angular) * Math.Cos(lat1),
            Math.Cos(angular) - Math.Sin(lat1) * Math.Sin(lat2));

        return new GeoPoint(lat2 / rad, lon2 / rad);
    }
}
