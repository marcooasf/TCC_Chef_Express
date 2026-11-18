using System.Text.Json.Serialization;
using ChefExpress.Api.OpenApi;
using ChefExpress.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ChefExpress.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public const string FrontendCorsPolicy = "Frontend";

    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers()
            .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddOpenApi(o =>
        {
            o.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            o.AddOperationTransformer<BearerSecuritySchemeTransformer>();
        });
        services.AddProblemDetails();
        services.AddExceptionHandler<Infrastructure.GlobalExceptionHandler>();

        services.AddFrontendCors(configuration);
        services.AddJwtAuthentication();

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

    /// <summary>
    /// Stateless JWT (Bearer) auth. Uses the same <see cref="JwtOptions"/> that signs tokens
    /// (registered by AddInfrastructure). Protect endpoints with [Authorize] or
    /// [Authorize(Roles = nameof(UserRole.Professional))].
    /// </summary>
    private static void AddJwtAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<JwtOptions>((bearer, jwt) =>
            {
                bearer.MapInboundClaims = false; // keep "sub"/"role" as issued
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = jwt.GetSigningKey(),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = JwtRegisteredClaimNames.Name,
                    RoleClaimType = AuthClaims.Role
                };
            });

        services.AddAuthorization();
    }
}
