namespace ChefExpress.Domain.Geography;

/// <summary>
/// Latitude/longitude rectangle that encloses a circle around a point.
/// Used as a cheap, index-friendly pre-filter before the exact distance check in SQL.
/// </summary>
public readonly record struct GeoBoundingBox(
    double MinLatitude,
    double MaxLatitude,
    double MinLongitude,
    double MaxLongitude)
{
    /// <remarks>
    /// Bounding-circle method from J. P. Matuschek, "Finding Points Within a Distance of a
    /// Latitude/Longitude Using Bounding Coordinates". Near the poles, or if the box would cross
    /// the ±180° meridian, it falls back to the full longitude range (never the case in Brazil).
    /// </remarks>
    public static GeoBoundingBox Around(GeoPoint center, double radiusKm)
    {
        if (!(radiusKm > 0))
            throw new ArgumentOutOfRangeException(nameof(radiusKm), radiusKm, "Radius must be greater than zero.");

        var angularRadius = radiusKm / GeoPoint.EarthRadiusKm;          // radians
        var deltaLat = angularRadius / GeoPoint.DegreesToRadians;       // degrees

        var minLat = center.Latitude - deltaLat;
        var maxLat = center.Latitude + deltaLat;

        if (minLat <= -90 || maxLat >= 90)
            return new(Math.Max(minLat, -90), Math.Min(maxLat, 90), -180, 180);

        var sinRatio = Math.Sin(angularRadius) / Math.Cos(center.Latitude * GeoPoint.DegreesToRadians);
        if (sinRatio >= 1)
            return new(minLat, maxLat, -180, 180);

        var deltaLon = Math.Asin(sinRatio) / GeoPoint.DegreesToRadians;
        var minLon = center.Longitude - deltaLon;
        var maxLon = center.Longitude + deltaLon;

        if (minLon < -180 || maxLon > 180)
            return new(minLat, maxLat, -180, 180);

        return new(minLat, maxLat, minLon, maxLon);
    }

    public bool Contains(GeoPoint point) =>
        point.Latitude >= MinLatitude && point.Latitude <= MaxLatitude &&
        point.Longitude >= MinLongitude && point.Longitude <= MaxLongitude;
}
