using ChefExpress.Application.Abstractions.Persistence;
using ChefExpress.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChefExpress.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var dbOptions = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();
        services.AddSingleton(Options.Create(dbOptions));

        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'ConnectionStrings:Default' is not configured.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(
                connectionString,
                new MariaDbServerVersion(Version.Parse(dbOptions.MariaDbVersion)),
                mysql => mysql.EnableRetryOnFailure(maxRetryCount: 5)));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        // Repositories (one per module) are registered here, e.g.:
        // services.AddScoped<IProfessionalProfileRepository, ProfessionalProfileRepository>();

        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

        return services;
    }

    /// <summary>Applies pending migrations if <c>Database:ApplyMigrationsOnStartup</c> is true.</summary>
    public static async Task ApplyMigrationsIfEnabledAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        if (!options.ApplyMigrationsOnStartup)
            return;

        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DependencyInjection));
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        logger.LogInformation("Applying database migrations...");
        await db.Database.MigrateAsync();
        logger.LogInformation("Database is up to date.");
    }
}
