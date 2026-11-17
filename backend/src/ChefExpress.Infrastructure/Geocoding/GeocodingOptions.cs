namespace ChefExpress.Infrastructure.Geocoding;

/// <summary>
/// Settings for the OpenStreetMap Nominatim client ("Geocoding" section).
/// Public server usage policy: https://operations.osmfoundation.org/policies/nominatim/
/// (max 1 request/s, identifying User-Agent, cache results, no bulk geocoding).
/// </summary>
public sealed class GeocodingOptions
{
    public const string SectionName = "Geocoding";

    /// <summary>When false, every lookup returns <c>Geocoding.Unavailable</c> and no HTTP calls are made.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Public server by default; point it to a self-hosted Nominatim to lift the rate limit.</summary>
    public string BaseUrl { get; init; } = "https://nominatim.openstreetmap.org/";

    /// <summary>Must identify the application (required by the usage policy).</summary>
    public string UserAgent { get; init; } = "ChefExpress/1.0 (TCC Devs2Blu)";

    /// <summary>Optional contact e-mail, sent as <c>email=</c> so OSM can reach us instead of blocking.</summary>
    public string? ContactEmail { get; init; }

    /// <summary>1 for the public server. Use a higher value, or 0 (no limit), for a self-hosted instance.</summary>
    public int MaxRequestsPerSecond { get; init; } = 1;

    /// <summary>How long a successful lookup is cached (in memory).</summary>
    public TimeSpan CacheDuration { get; init; } = TimeSpan.FromDays(7);

    /// <summary>How long a "not found" answer is cached, so typos don't hammer the server.</summary>
    public TimeSpan NotFoundCacheDuration { get; init; } = TimeSpan.FromHours(1);

    /// <summary>Upper bound for one HTTP call to the map service, including retries and the rate-limit queue.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(10);
}
