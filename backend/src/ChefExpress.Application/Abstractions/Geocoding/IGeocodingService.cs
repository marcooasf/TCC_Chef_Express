using ChefExpress.Application.Common;
using ChefExpress.Domain.Geography;

namespace ChefExpress.Application.Abstractions.Geocoding;

/// <summary>
/// Turns an address into coordinates (port). The adapter is OpenStreetMap's Nominatim
/// (<c>Infrastructure/Geocoding</c>). Expected failures come back as <see cref="GeocodingErrors"/>
/// instead of exceptions, so callers can save the profile anyway.
/// </summary>
public interface IGeocodingService
{
    Task<Result<GeocodedPlace>> GeocodeAsync(AddressQuery address, CancellationToken cancellationToken = default);
}

/// <param name="State">UF code, e.g. "SC".</param>
public sealed record AddressQuery(string City, string State, string? Neighborhood = null);

public enum GeocodingPrecision
{
    /// <summary>Centre of the city (the neighbourhood was not given or not found).</summary>
    City,
    Neighborhood
}

/// <param name="DisplayName">Full name returned by the map service, e.g. "Velha, Blumenau, Santa Catarina, ...".</param>
public sealed record GeocodedPlace(GeoPoint Point, GeocodingPrecision Precision, string DisplayName);

public static class GeocodingErrors
{
    public static readonly Error MissingAddress = new(
        "Geocoding.MissingAddress",
        "Informe cidade e UF para localizar o perfil no mapa.",
        ErrorType.Validation);

    public static readonly Error NotFound = new(
        "Geocoding.NotFound",
        "Não encontramos este endereço no mapa. Confira a cidade, a UF e o bairro.",
        ErrorType.NotFound);

    public static readonly Error Unavailable = new(
        "Geocoding.Unavailable",
        "O serviço de mapas está indisponível no momento. Tente novamente mais tarde.",
        ErrorType.Unavailable);
}
