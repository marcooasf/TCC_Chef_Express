using ChefExpress.Application.Professionals;
using Microsoft.Extensions.DependencyInjection;

namespace ChefExpress.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Use-case services (one per module) are registered here, e.g.:
        // services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IProfileLocationService, ProfileLocationService>();
        return services;
    }
}
