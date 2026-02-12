using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Winnow.Server.Infrastructure.Persistence;

public class SqliteVectorConnectionInterceptor : DbConnectionInterceptor
{
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        LoadExtension(connection);
        base.ConnectionOpened(connection, eventData);
    }

    public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        LoadExtension(connection);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    private static void LoadExtension(DbConnection connection)
    {
        if (connection is SqliteConnection sqliteConnection)
        {
            sqliteConnection.EnableExtensions(true);
            try
            {
                // Try to load from base directory explicitly
                var extensionPath = Path.Combine(AppContext.BaseDirectory, "vec0");
                sqliteConnection.LoadExtension(extensionPath);
            }
            catch (Exception ex)
            {
                // Log or rethrow? For now, rethrow to fail fast if vector search is critical.
                // Fallback to just "vec0" if path-based fails (though path-based is usually better)
                try
                {
                    sqliteConnection.LoadExtension("vec0");
                }
                catch
                {
                    Console.WriteLine($"Failed to load sqlite-vec extension: {ex.Message}");
                    throw;
                }
            }
        }
    }
}
