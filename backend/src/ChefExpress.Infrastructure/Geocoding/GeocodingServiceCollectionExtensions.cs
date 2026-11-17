using ChefExpress.Application.Abstractions.Geocoding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ChefExpress.Infrastructure.Geocoding;

internal static class GeocodingServiceCollectionExtensions
{
    public static IServiceCollection AddGeocoding(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(GeocodingOptions.SectionName).Get<GeocodingOptions>() ?? new GeocodingOptions();
        services.AddSingleton(Options.Create(options));

        if (!options.Enabled)
        {
            services.AddSingleton<IGeocodingService, DisabledGeocodingService>();
            return services;
        }

        services.AddMemoryCache();

        var baseUrl = options.BaseUrl.EndsWith('/') ? options.BaseUrl : options.BaseUrl + "/";

        var client = services.AddHttpClient<IGeocodingService, NominatimGeocodingService>(http =>
        {
            http.BaseAddress = new Uri(baseUrl);
            http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", options.UserAgent);
            http.Timeout = Timeout.InfiniteTimeSpan; // the resilience pipeline owns timeouts
        });

        // Outer handler: total timeout -> retry (5xx, 408, 429, network errors) -> circuit breaker -> attempt timeout.
        client.AddStandardResilienceHandler(resilience =>
        {
            resilience.TotalRequestTimeout.Timeout = options.Timeout;
            resilience.AttemptTimeout.Timeout = options.Timeout / 2;
            resilience.Retry.MaxRetryAttempts = 2;
            resilience.CircuitBreaker.SamplingDuration = Max(TimeSpan.FromSeconds(30), options.Timeout);
        });

        // Inner handler: every attempt (including retries) waits for the rate limiter.
        if (options.MaxRequestsPerSecond > 0)
        {
            services.AddSingleton(new GeocodingRateLimiter(options.MaxRequestsPerSecond));
            services.AddTransient<GeocodingRateLimitHandler>();
            client.AddHttpMessageHandler<GeocodingRateLimitHandler>();
        }

        return services;
    }

    private static TimeSpan Max(TimeSpan a, TimeSpan b) => a > b ? a : b;
}
