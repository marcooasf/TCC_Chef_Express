using System.Text.Json.Serialization;

namespace ChefExpress.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public const string FrontendCorsPolicy = "Frontend";

    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers()
            .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddOpenApi();
        services.AddProblemDetails();
        services.AddExceptionHandler<Infrastructure.GlobalExceptionHandler>();

        services.AddFrontendCors(configuration);

        return services;
    }

    /// <summary>
    /// CORS for the Angular app (risk #1 in the planning doc).
    /// Origins come from "Cors:AllowedOrigins" — e.g. http://localhost:4200.
    /// </summary>
    private static void AddFrontendCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options => options.AddPolicy(FrontendCorsPolicy, policy =>
            policy.WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod()));
    }
}
