using ChefExpress.Application;
using ChefExpress.Application.Abstractions.Geocoding;
using ChefExpress.Application.Professionals;
using ChefExpress.Infrastructure;
using ChefExpress.Infrastructure.Geocoding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChefExpress.UnitTests.Infrastructure;

public sealed class GeocodingRegistrationTests
{
    [Fact]
    public void Nominatim_client_is_registered_by_default()
    {
        using var provider = BuildProvider([]);

        Assert.IsType<NominatimGeocodingService>(provider.GetRequiredService<IGeocodingService>());
        Assert.NotNull(provider.GetRequiredService<GeocodingRateLimiter>());

        using var scope = provider.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IProfileLocationService>());
    }

    [Fact]
    public void Self_hosted_server_without_rate_limit_skips_the_limiter()
    {
        using var provider = BuildProvider(new()
        {
            ["Geocoding:BaseUrl"] = "http://nominatim:8080",
            ["Geocoding:MaxRequestsPerSecond"] = "0"
        });

        Assert.IsType<NominatimGeocodingService>(provider.GetRequiredService<IGeocodingService>());
        Assert.Null(provider.GetService<GeocodingRateLimiter>());
    }

    [Fact]
    public async Task Disabled_geocoding_never_calls_the_network()
    {
        using var provider = BuildProvider(new() { ["Geocoding:Enabled"] = "false" });

        var service = provider.GetRequiredService<IGeocodingService>();
        var result = await service.GeocodeAsync(new AddressQuery("Blumenau", "SC"));

        Assert.IsType<DisabledGeocodingService>(service);
        Assert.Equal(GeocodingErrors.Unavailable, result.Error);
    }

    private static ServiceProvider BuildProvider(Dictionary<string, string?> settings)
    {
        settings["ConnectionStrings:Default"] = "Server=localhost;Database=unused";
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        return new ServiceCollection()
            .AddLogging()
            .AddApplication()
            .AddInfrastructure(configuration)
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }
}
