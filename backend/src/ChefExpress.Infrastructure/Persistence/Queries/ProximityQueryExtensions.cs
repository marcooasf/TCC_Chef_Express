using System.Linq.Expressions;
using ChefExpress.Domain.Geography;
using ChefExpress.Domain.Professionals;

namespace ChefExpress.Infrastructure.Persistence.Queries;

/// <summary>
/// Building blocks for the proximity filter of the search (task 9). They run fully in SQL:
/// a bounding box (uses the Latitude/Longitude index) and then the exact haversine distance.
/// Profiles without coordinates are excluded; the search should fall back to the city/UF filter.
/// </summary>
/// <example>
/// <code>
/// var origin = new GeoPoint(request.Latitude, request.Longitude); // browser geolocation
/// var page = await db.ProfessionalProfiles
///     .WithinRadius(origin, request.RadiusKm)
///     .OrderByDistanceFrom(origin)
///     .Skip(skip).Take(take)
///     .ToListAsync(ct);
/// // Distance shown to the user ("~3 km de você"): computed in memory
/// var km = origin.DistanceKmTo(profile.Location!.Value);
/// </code>
/// </example>
public static class ProximityQueryExtensions
{
    private const double DegreesToRadians = Math.PI / 180.0;

    public static IQueryable<ProfessionalProfile> WithinRadius(
        this IQueryable<ProfessionalProfile> query, GeoPoint origin, double radiusKm)
    {
        var box = GeoBoundingBox.Around(origin, radiusKm);
        double minLat = box.MinLatitude, maxLat = box.MaxLatitude;
        double minLon = box.MinLongitude, maxLon = box.MaxLongitude;

        var distance = DistanceKmFrom(origin);
        var withinRadius = Expression.Lambda<Func<ProfessionalProfile, bool>>(
            Expression.LessThanOrEqual(distance.Body, Expression.Constant(radiusKm)),
            distance.Parameters);

        return query
            .Where(p => p.Latitude != null && p.Longitude != null
                && p.Latitude >= minLat && p.Latitude <= maxLat
                && p.Longitude >= minLon && p.Longitude <= maxLon)
            .Where(withinRadius);
    }

    public static IOrderedQueryable<ProfessionalProfile> OrderByDistanceFrom(
        this IQueryable<ProfessionalProfile> query, GeoPoint origin) =>
        query.OrderBy(DistanceKmFrom(origin));

    /// <summary>
    /// Haversine distance (km) from <paramref name="origin"/> to the profile, translatable to SQL.
    /// Only valid for profiles with coordinates (apply <see cref="WithinRadius"/> first).
    /// </summary>
    public static Expression<Func<ProfessionalProfile, double>> DistanceKmFrom(GeoPoint origin)
    {
        var originLat = origin.Latitude * DegreesToRadians;
        var originLon = origin.Longitude * DegreesToRadians;
        var cosOriginLat = Math.Cos(originLat);
        const double diameterKm = 2 * GeoPoint.EarthRadiusKm;

        return p => diameterKm * Math.Asin(Math.Sqrt(
            Math.Pow(Math.Sin((p.Latitude!.Value * DegreesToRadians - originLat) / 2), 2) +
            cosOriginLat * Math.Cos(p.Latitude!.Value * DegreesToRadians) *
            Math.Pow(Math.Sin((p.Longitude!.Value * DegreesToRadians - originLon) / 2), 2)));
    }
}
