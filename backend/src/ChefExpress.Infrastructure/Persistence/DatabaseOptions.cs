namespace ChefExpress.Infrastructure.Persistence;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>MariaDB server version used by the Pomelo provider (e.g. "11.4.0").</summary>
    public string MariaDbVersion { get; init; } = "11.8.0";

    /// <summary>Apply pending EF Core migrations when the API starts (handy for Docker/dev).</summary>
    public bool ApplyMigrationsOnStartup { get; init; }
}
