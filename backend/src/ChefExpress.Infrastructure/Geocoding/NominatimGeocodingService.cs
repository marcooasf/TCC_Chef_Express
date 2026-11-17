using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ChefExpress.Application.Abstractions.Geocoding;
using ChefExpress.Application.Common;
using ChefExpress.Domain.Geography;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace ChefExpress.Infrastructure.Geocoding;

/// <summary>
/// <see cref="IGeocodingService"/> backed by OpenStreetMap Nominatim.
/// <list type="number">
/// <item>Structured search for the city (<c>city=…&amp;state=…</c>), which also gives its bounding box.</item>
/// <item>If a neighbourhood was given, a search for it restricted to that bounding box
/// (<c>viewbox</c> + <c>bounded=1</c>, settlement features only, so streets never match).
/// If it is not found, the city centre is used.</item>
/// </list>
/// Responses are cached; rate limiting and retries are configured on the HttpClient (see DependencyInjection).
/// </summary>
internal sealed class NominatimGeocodingService(
    HttpClient http,
    IMemoryCache cache,
    IOptions<GeocodingOptions> options,
    ILogger<NominatimGeocodingService> logger) : IGeocodingService
{
    private readonly GeocodingOptions _options = options.Value;

    public async Task<Result<GeocodedPlace>> GeocodeAsync(AddressQuery address, CancellationToken cancellationToken = default)
    {
        var stateName = BrazilianStates.GetName(address.State);
        if (string.IsNullOrWhiteSpace(address.City) || stateName is null)
            return GeocodingErrors.MissingAddress;

        try
        {
            var city = await SearchAsync(CityQuery(address.City.Trim(), stateName), cancellationToken);
            if (city is null)
            {
                logger.LogInformation("City not found on Nominatim: {City}/{State}", address.City, address.State);
                return GeocodingErrors.NotFound;
            }

            if (!string.IsNullOrWhiteSpace(address.Neighborhood) && city.BoundingBox is { } cityBox)
            {
                var neighborhood = await SearchAsync(NeighborhoodQuery(address.Neighborhood.Trim(), cityBox), cancellationToken);
                if (neighborhood is not null)
                    return new GeocodedPlace(neighborhood.Point, GeocodingPrecision.Neighborhood, neighborhood.DisplayName);

                logger.LogInformation(
                    "Neighborhood {Neighborhood} not found in {City}/{State}; using the city centre",
                    address.Neighborhood, address.City, address.State);
            }

            return new GeocodedPlace(city.Point, GeocodingPrecision.City, city.DisplayName);
        }
        catch (Exception ex) when (IsTransientFailure(ex, cancellationToken))
        {
            logger.LogWarning(ex, "Nominatim lookup failed for {City}/{State}", address.City, address.State);
            return GeocodingErrors.Unavailable;
        }
    }

    private static bool IsTransientFailure(Exception ex, CancellationToken cancellationToken) => ex switch
    {
        HttpRequestException => true,
        ExecutionRejectedException => true,          // Polly: timeout, open circuit, rate limiter rejection
        JsonException or FormatException or ArgumentOutOfRangeException => true, // unexpected response body
        OperationCanceledException => !cancellationToken.IsCancellationRequested, // HttpClient timeout, not the caller
        _ => false
    };

    /// <summary>Returns the first match, or null if there is none. Both outcomes are cached.</summary>
    private async Task<NominatimPlace?> SearchAsync(string relativeUri, CancellationToken cancellationToken)
    {
        var cacheKey = "nominatim:" + relativeUri;
        if (cache.TryGetValue(cacheKey, out NominatimPlace? cached))
            return cached;

        using var response = await http.GetAsync(relativeUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        var results = await response.Content.ReadFromJsonAsync<NominatimResult[]>(cancellationToken)
            ?? throw new JsonException("Nominatim returned an empty body.");

        var place = results.Length == 0 ? null : results[0].ToPlace();

        cache.Set(cacheKey, place, place is null ? _options.NotFoundCacheDuration : _options.CacheDuration);
        return place;
    }

    private string CityQuery(string city, string stateName) => BuildQuery(
        ("city", city),
        ("state", stateName),
        ("country", "Brasil"));

    private string NeighborhoodQuery(string neighborhood, GeoBoundingBox cityBox) => BuildQuery(
        ("q", neighborhood),
        // viewbox = <left>,<top>,<right>,<bottom> = minLon,maxLat,maxLon,minLat
        ("viewbox", string.Join(',',
            Invariant(cityBox.MinLongitude), Invariant(cityBox.MaxLatitude),
            Invariant(cityBox.MaxLongitude), Invariant(cityBox.MinLatitude))),
        ("bounded", "1"));

    private string BuildQuery(params (string Key, string Value)[] parameters)
    {
        var query = new StringBuilder("search?format=jsonv2&limit=1&countrycodes=br&featureType=settlement&accept-language=pt-BR");
        foreach (var (key, value) in parameters)
            query.Append('&').Append(key).Append('=').Append(Uri.EscapeDataString(value));

        if (!string.IsNullOrWhiteSpace(_options.ContactEmail))
            query.Append("&email=").Append(Uri.EscapeDataString(_options.ContactEmail));

        return query.ToString();
    }

    private static string Invariant(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    private sealed record NominatimPlace(GeoPoint Point, string DisplayName, GeoBoundingBox? BoundingBox);

    /// <summary>Subset of a <c>format=jsonv2</c> result. Nominatim sends numbers as strings.</summary>
    private sealed class NominatimResult
    {
        [JsonPropertyName("lat")] public string Lat { get; init; } = "";
        [JsonPropertyName("lon")] public string Lon { get; init; } = "";
        [JsonPropertyName("display_name")] public string DisplayName { get; init; } = "";

        /// <summary>[minLat, maxLat, minLon, maxLon]</summary>
        [JsonPropertyName("boundingbox")] public string[]? BoundingBox { get; init; }

        public NominatimPlace ToPlace()
        {
            var point = new GeoPoint(Parse(Lat), Parse(Lon));

            GeoBoundingBox? box = BoundingBox is { Length: 4 } b
                ? new GeoBoundingBox(Parse(b[0]), Parse(b[1]), Parse(b[2]), Parse(b[3]))
                : null;

            return new NominatimPlace(point, DisplayName, box);
        }

        private static double Parse(string value) => double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
    }
}
