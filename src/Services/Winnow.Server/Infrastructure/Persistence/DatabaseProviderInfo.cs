namespace Winnow.Server.Infrastructure.Persistence;

/// <summary>
/// Holds the configured database provider name ("Postgres" or "Sqlite").
/// Registered as a singleton so any component can resolve it without re-reading IConfiguration.
/// </summary>
public record DatabaseProviderInfo(string Provider)
{
    public bool IsPostgres => Provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase);
    public bool IsSqlite => Provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase);
}
