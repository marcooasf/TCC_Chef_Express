using System.Threading.RateLimiting;

namespace ChefExpress.Infrastructure.Geocoding;

/// <summary>Process-wide limiter for calls to the map service (singleton).</summary>
internal sealed class GeocodingRateLimiter(int maxRequestsPerSecond) : IDisposable
{
    public RateLimiter Limiter { get; } = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
    {
        TokenLimit = maxRequestsPerSecond,
        TokensPerPeriod = maxRequestsPerSecond,
        ReplenishmentPeriod = TimeSpan.FromSeconds(1),
        QueueLimit = 100,
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        AutoReplenishment = true
    });

    public void Dispose() => Limiter.Dispose();
}

/// <summary>
/// Waits for a token before every outgoing request. Registered inside the resilience handler,
/// so each retry attempt is throttled as well.
/// </summary>
internal sealed class GeocodingRateLimitHandler(GeocodingRateLimiter rateLimiter) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var lease = await rateLimiter.Limiter.AcquireAsync(1, cancellationToken);
        if (!lease.IsAcquired)
            throw new HttpRequestException("Geocoding rate-limit queue is full.");

        return await base.SendAsync(request, cancellationToken);
    }
}
