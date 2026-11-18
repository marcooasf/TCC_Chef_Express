using ChefExpress.Application.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace ChefExpress.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Use-case services (one per module)
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}
