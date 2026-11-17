using ChefExpress.Application.Abstractions.Geocoding;
using ChefExpress.Application.Common;
using ChefExpress.Domain.Professionals;

namespace ChefExpress.Application.Professionals;

public interface IProfileLocationService
{
    /// <summary>
    /// Geocodes the profile's address and stores the approximate coordinates on it.
    /// Does not save: the caller (e.g. the profile update use case, tasks 6–8) commits with
    /// <c>IUnitOfWork</c>. On failure the location is cleared and the error is returned. The
    /// profile is still valid and should be saved anyway; search falls back to city/UF.
    /// </summary>
    /// <example>
    /// <code>
    /// if (profile.UpdateAddress(dto.City, dto.State, dto.Neighborhood))
    ///     await locationService.RefreshLocationAsync(profile, ct); // result is optional to show to the user
    /// await unitOfWork.SaveChangesAsync(ct);
    /// </code>
    /// </example>
    Task<Result<GeocodedPlace>> RefreshLocationAsync(ProfessionalProfile profile, CancellationToken cancellationToken = default);
}

internal sealed class ProfileLocationService(IGeocodingService geocoding) : IProfileLocationService
{
    public async Task<Result<GeocodedPlace>> RefreshLocationAsync(
        ProfessionalProfile profile, CancellationToken cancellationToken = default)
    {
        if (!profile.HasAddress)
        {
            profile.ClearLocation();
            return GeocodingErrors.MissingAddress;
        }

        var result = await geocoding.GeocodeAsync(
            new AddressQuery(profile.City, profile.State, profile.Neighborhood), cancellationToken);

        if (result.IsSuccess)
            profile.SetLocation(result.Value.Point);
        else
            profile.ClearLocation();

        return result;
    }
}
