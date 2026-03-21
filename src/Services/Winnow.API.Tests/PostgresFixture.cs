using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Npgsql;
using Xunit;

namespace Winnow.API.Tests;

public class PostgresFixture : IAsyncLifetime
{
    private const string DbUser = "postgres";
    private const string DbPassword = "postgres";
    private const string DbName = "postgres";
    private const int PostgresPort = 5432;

    private IContainer _container = default!;

    /// <summary>
    /// Provides the connection string for the running PostgreSQL container.
    /// </summary>
    public string ConnectionString { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        var podmanSocket = FindPodmanSocket();

        // Use raw ContainerBuilder (not PostgreSqlBuilder) to avoid exec-based wait strategies
        // that fail on Podman with "Connection reset by peer".
        // We handle readiness ourselves via Npgsql polling.
        var builder = new ContainerBuilder()
            .WithImage("pgvector/pgvector:pg18")
            .WithPortBinding(PostgresPort, true)
            .WithEnvironment("POSTGRES_USER", DbUser)
            .WithEnvironment("POSTGRES_PASSWORD", DbPassword)
            .WithEnvironment("POSTGRES_DB", DbName);

        if (podmanSocket != null)
        {
            var socketUri = $"unix://{podmanSocket}";
            builder = builder.WithDockerEndpoint(socketUri);
            Environment.SetEnvironmentVariable("DOCKER_HOST", socketUri);
            Environment.SetEnvironmentVariable("TESTCONTAINERS_RYUK_DISABLED", "true");
            Environment.SetEnvironmentVariable("TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE", podmanSocket);
        }

        _container = builder.Build();

        try
        {
            await _container.StartAsync();
        }
        catch (IOException)
        {
            // Podman exec-based readiness checks may throw "Connection reset by peer"
            // but the container itself is still running. We'll verify with Npgsql below.
        }

        // Build connection string from the mapped port
        var mappedPort = _container.GetMappedPublicPort(PostgresPort);
        var host = _container.Hostname;
        ConnectionString = $"Host={host};Port={mappedPort};Database={DbName};Username={DbUser};Password={DbPassword}";

        // Poll until PostgreSQL actually accepts connections
        for (var i = 0; i < 30; i++)
        {
            try
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT 1";
                await cmd.ExecuteScalarAsync();
                return; // Ready!
            }
            catch
            {
                await Task.Delay(1000);
            }
        }

        throw new InvalidOperationException("PostgreSQL container did not become ready within 30 seconds.");
    }

    private static string? FindPodmanSocket()
    {
        // Check rootful socket
        if (File.Exists("/run/podman/podman.sock"))
            return "/run/podman/podman.sock";

        // Check rootless socket at /run/user/{uid}/podman/podman.sock
        var uid = Environment.GetEnvironmentVariable("UID");
        if (string.IsNullOrEmpty(uid))
        {
            try { uid = File.ReadAllText("/proc/self/loginuid").Trim(); }
            catch { uid = "1000"; }
        }

        var userSocket = $"/run/user/{uid}/podman/podman.sock";
        if (File.Exists(userSocket))
            return userSocket;

        // Check via XDG_RUNTIME_DIR
        var xdgRuntime = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
        if (!string.IsNullOrEmpty(xdgRuntime))
        {
            var xdgSocket = Path.Combine(xdgRuntime, "podman", "podman.sock");
            if (File.Exists(xdgSocket))
                return xdgSocket;
        }

        return null;
    }

    public async Task DisposeAsync()
    {
        if (_container != null)
        {
            await _container.StopAsync();
        }
    }
}

[CollectionDefinition("PostgresCollection")]
public class PostgresCollection : ICollectionFixture<PostgresFixture>
{
}
