using ChefExpress.Application.Abstractions.Geocoding;
using ChefExpress.Application.Common;

namespace ChefExpress.Infrastructure.Geocoding;

/// <summary>Used when <c>Geocoding:Enabled</c> is false (offline development, tests).</summary>
internal sealed class DisabledGeocodingService : IGeocodingService
{
    public Task<Result<GeocodedPlace>> GeocodeAsync(AddressQuery address, CancellationToken cancellationToken = default) =>
        Task.FromResult<Result<GeocodedPlace>>(GeocodingErrors.Unavailable);
}
