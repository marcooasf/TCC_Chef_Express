using ChefExpress.Domain.Geography;
using ChefExpress.Domain.Professionals;
using ChefExpress.Domain.Users;
using ChefExpress.Infrastructure.Persistence;
using ChefExpress.Infrastructure.Persistence.Queries;
using Microsoft.EntityFrameworkCore;

namespace ChefExpress.UnitTests.Infrastructure;

public sealed class ProximityQueryTests
{
    private static readonly GeoPoint BlumenauCentre = new(-26.9196, -49.0658);

    [Fact]
    public void Radius_filter_keeps_only_nearby_profiles_ordered_by_distance()
    {
        var velha = ProfileAt(-26.918, -49.093);      // ~2.7 km
        var garcia = ProfileAt(-26.950, -49.075);     // ~3.5 km
        var gaspar = ProfileAt(-26.931, -48.958);     // ~10.8 km
        var floripa = ProfileAt(-27.595, -48.548);    // ~88 km
        var noLocation = ProfessionalProfile.CreateFor(User.Create("Sem", "sem@example.com"));

        var result = new[] { floripa, gaspar, noLocation, garcia, velha }
            .AsQueryable()
            .WithinRadius(BlumenauCentre, 5)
            .OrderByDistanceFrom(BlumenauCentre)
            .ToList();

        Assert.Equal([velha, garcia], result);
    }

    [Fact]
    public void Sql_distance_expression_matches_the_domain_formula()
    {
        var profile = ProfileAt(-27.595, -48.548);
        var sqlDistance = ProximityQueryExtensions.DistanceKmFrom(BlumenauCentre).Compile()(profile);

        Assert.Equal(BlumenauCentre.DistanceKmTo(profile.Location!.Value), sqlDistance, precision: 6);
    }

    [Fact]
    public void Proximity_query_is_fully_translated_to_MariaDb_sql()
    {
        // No connection is opened: ToQueryString only runs the SQL translation.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql("Server=localhost;Database=unused", new MariaDbServerVersion(new Version(11, 8, 0)))
            .Options;
        using var db = new AppDbContext(options);

        var sql = db.ProfessionalProfiles
            .WithinRadius(BlumenauCentre, 10)
            .OrderByDistanceFrom(BlumenauCentre)
            .Take(20)
            .ToQueryString();

        Assert.Contains("`p`.`Latitude` >= ", sql);
        Assert.Contains("`p`.`Longitude` <= ", sql);
        Assert.Contains("ASIN(SQRT(", sql);
        Assert.Contains("ORDER BY", sql);
    }

    private static ProfessionalProfile ProfileAt(double lat, double lon)
    {
        var profile = ProfessionalProfile.CreateFor(User.Create("Chef", "chef@example.com"));
        profile.SetLocation(new GeoPoint(lat, lon));
        return profile;
    }
}
