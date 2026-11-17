using System.Net;
using System.Text;
using ChefExpress.Application.Abstractions.Geocoding;
using ChefExpress.Infrastructure.Geocoding;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ChefExpress.UnitTests.Infrastructure;

public sealed class NominatimGeocodingServiceTests
{
    // Trimmed real responses (format=jsonv2)
    private const string BlumenauJson = """
        [{"lat":"-26.9195567","lon":"-49.0658025","display_name":"Blumenau, Santa Catarina, Região Sul, Brasil",
          "boundingbox":["-27.1320000","-26.6128012","-49.2007158","-49.0096552"]}]
        """;

    private const string VelhaJson = """
        [{"lat":"-26.9184933","lon":"-49.0925069","display_name":"Velha, Blumenau, Santa Catarina, Região Sul, Brasil",
          "boundingbox":["-26.9401749","-26.9116024","-49.1201621","-49.0783023"]}]
        """;

    [Fact]
    public async Task City_only_address_returns_the_city_centre()
    {
        var handler = new StubHandler(_ => Json(BlumenauJson));

        var result = await CreateService(handler).GeocodeAsync(new AddressQuery("Blumenau", "SC"));

        Assert.True(result.IsSuccess);
        Assert.Equal(GeocodingPrecision.City, result.Value.Precision);
        Assert.Equal(-26.9195567, result.Value.Point.Latitude);
        Assert.Equal(-49.0658025, result.Value.Point.Longitude);

        var request = Assert.Single(handler.Requests);
        Assert.Contains("city=Blumenau", request);
        Assert.Contains("state=Santa%20Catarina", request);  // UF expanded to the full name
        Assert.Contains("countrycodes=br", request);
        Assert.Contains("format=jsonv2", request);
    }

    [Fact]
    public async Task Neighborhood_is_searched_inside_the_city_bounding_box()
    {
        var handler = new StubHandler(uri => Json(uri.Contains("q=Velha") ? VelhaJson : BlumenauJson));

        var result = await CreateService(handler).GeocodeAsync(new AddressQuery("Blumenau", "SC", "Velha"));

        Assert.True(result.IsSuccess);
        Assert.Equal(GeocodingPrecision.Neighborhood, result.Value.Precision);
        Assert.Equal(-26.9184933, result.Value.Point.Latitude);

        Assert.Equal(2, handler.Requests.Count);
        var neighborhoodRequest = handler.Requests[1];
        // viewbox = minLon,maxLat,maxLon,minLat of Blumenau
        Assert.Contains("viewbox=-49.2007158%2C-26.6128012%2C-49.0096552%2C-27.132", neighborhoodRequest);
        Assert.Contains("bounded=1", neighborhoodRequest);
        Assert.Contains("featureType=settlement", neighborhoodRequest);
    }

    [Fact]
    public async Task Unknown_neighborhood_falls_back_to_the_city_centre()
    {
        var handler = new StubHandler(uri => Json(uri.Contains("q=") ? "[]" : BlumenauJson));

        var result = await CreateService(handler).GeocodeAsync(new AddressQuery("Blumenau", "SC", "Bairro Inexistente"));

        Assert.True(result.IsSuccess);
        Assert.Equal(GeocodingPrecision.City, result.Value.Precision);
    }

    [Fact]
    public async Task Unknown_city_returns_not_found()
    {
        var result = await CreateService(new StubHandler(_ => Json("[]"))).GeocodeAsync(new AddressQuery("Xyzzyville", "SC"));

        Assert.Equal(GeocodingErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Invalid_state_is_rejected_without_calling_the_server()
    {
        var handler = new StubHandler(_ => Json(BlumenauJson));

        var result = await CreateService(handler).GeocodeAsync(new AddressQuery("Blumenau", "XX"));

        Assert.Equal(GeocodingErrors.MissingAddress, result.Error);
        Assert.Empty(handler.Requests);
    }

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable, "")]
    [InlineData(HttpStatusCode.TooManyRequests, "")]
    [InlineData(HttpStatusCode.OK, "<html>not json</html>")]
    public async Task Server_errors_and_bad_responses_become_unavailable(HttpStatusCode status, string body)
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(status) { Content = new StringContent(body) });

        var result = await CreateService(handler).GeocodeAsync(new AddressQuery("Blumenau", "SC"));

        Assert.Equal(GeocodingErrors.Unavailable, result.Error);
    }

    [Fact]
    public async Task Network_failure_becomes_unavailable()
    {
        var handler = new StubHandler(_ => throw new HttpRequestException("connection refused"));

        var result = await CreateService(handler).GeocodeAsync(new AddressQuery("Blumenau", "SC"));

        Assert.Equal(GeocodingErrors.Unavailable, result.Error);
    }

    [Fact]
    public async Task Caller_cancellation_is_not_swallowed()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var service = CreateService(new StubHandler(_ => Json(BlumenauJson)));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.GeocodeAsync(new AddressQuery("Blumenau", "SC"), cts.Token));
    }

    [Fact]
    public async Task Repeated_lookups_are_served_from_the_cache()
    {
        var handler = new StubHandler(uri => Json(uri.Contains("q=Velha") ? VelhaJson : BlumenauJson));
        var service = CreateService(handler);

        await service.GeocodeAsync(new AddressQuery("Blumenau", "SC", "Velha"));
        await service.GeocodeAsync(new AddressQuery("Blumenau", "SC", "Velha"));
        await service.GeocodeAsync(new AddressQuery("Blumenau", "SC")); // city already cached

        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task Not_found_answers_are_cached_too()
    {
        var handler = new StubHandler(_ => Json("[]"));
        var service = CreateService(handler);

        await service.GeocodeAsync(new AddressQuery("Xyzzyville", "SC"));
        await service.GeocodeAsync(new AddressQuery("Xyzzyville", "SC"));

        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Contact_email_is_sent_when_configured()
    {
        var handler = new StubHandler(_ => Json(BlumenauJson));
        var service = CreateService(handler, new GeocodingOptions { ContactEmail = "equipe@exemplo.com" });

        await service.GeocodeAsync(new AddressQuery("Blumenau", "SC"));

        Assert.Contains("email=equipe%40exemplo.com", Assert.Single(handler.Requests));
    }

    private static NominatimGeocodingService CreateService(StubHandler handler, GeocodingOptions? options = null) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("https://nominatim.test/") },
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(options ?? new GeocodingOptions()),
            NullLogger<NominatimGeocodingService>.Instance);

    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private sealed class StubHandler(Func<string, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<string> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var uri = request.RequestUri!.PathAndQuery;
            Requests.Add(uri);
            return Task.FromResult(respond(uri));
        }
    }
}
