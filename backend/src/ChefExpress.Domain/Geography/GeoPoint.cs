namespace ChefExpress.Domain.Geography;

/// <summary>A WGS-84 coordinate (the system used by OpenStreetMap and browser geolocation).</summary>
public readonly record struct GeoPoint
{
    /// <summary>Mean Earth radius (IUGG), used by every distance calculation in the app.</summary>
    public const double EarthRadiusKm = 6371.0088;

    internal const double DegreesToRadians = Math.PI / 180.0;

    public GeoPoint(double latitude, double longitude)
    {
        // Written as !(in range) so NaN is rejected too.
        if (!(latitude is >= -90 and <= 90))
            throw new ArgumentOutOfRangeException(nameof(latitude), latitude, "Latitude must be between -90 and 90.");
        if (!(longitude is >= -180 and <= 180))
            throw new ArgumentOutOfRangeException(nameof(longitude), longitude, "Longitude must be between -180 and 180.");

        Latitude = latitude;
        Longitude = longitude;
    }

    public double Latitude { get; }
    public double Longitude { get; }

    /// <summary>Great-circle (straight-line) distance using the haversine formula.</summary>
    public double DistanceKmTo(GeoPoint other)
    {
        var lat1 = Latitude * DegreesToRadians;
        var lat2 = other.Latitude * DegreesToRadians;
        var dLat = lat2 - lat1;
        var dLon = (other.Longitude - Longitude) * DegreesToRadians;

        var a = Math.Pow(Math.Sin(dLat / 2), 2)
              + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(dLon / 2), 2);

        return 2 * EarthRadiusKm * Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }

    /// <summary>Rounds both coordinates; 3 decimals ≈ 110 m, 2 decimals ≈ 1.1 km.</summary>
    public GeoPoint Round(int decimals) =>
        new(Math.Round(Latitude, decimals), Math.Round(Longitude, decimals));
}
