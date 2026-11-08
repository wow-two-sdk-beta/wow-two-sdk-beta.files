using DotNet.Testcontainers.Configurations;
using Npgsql;
using Testcontainers.PostgreSql;
using WoW.Two.Sdk.Beta.Be.Data.Migrations.Bespoke;
using WoW.Two.Sdk.Beta.Be.Testing.Containers.Postgres;

namespace Files.Tests.Lanes;

/// <summary>The PostgreSQL lane: one container for the test assembly, a fresh database per store.</summary>
/// <remarks>
///   - a test class joins the lane by <c>[Collection(PostgresLane.Collection)]</c>; the collection fixture owns the container
///   - with Docker unreachable the container never starts, and <see cref="FilesLaneTestFramework"/> reports every test of
///     the collection skipped with <see cref="SkipReason"/>, so the SQLite lane still runs without Docker;
///     <c>FILES_TESTS_POSTGRES=off</c> skips the lane on purpose, for the fast lane alone on a machine with Docker
///   - a database per store keeps tests apart and gives each its own Npgsql data source, whose enum types are loaded once
/// </remarks>
public sealed class PostgresLane : IFilesLane, IAsyncLifetime
{
    /// <summary>Holds the name of the collection every PostgreSQL-lane class joins.</summary>
    public const string Collection = "files-postgres";

    /// <summary>Holds the variable that, set to <c>off</c>, skips the lane although Docker answers.</summary>
    public const string SwitchVariable = "FILES_TESTS_POSTGRES";

    private static readonly Lazy<string?> Unreachable = new(ProbeDocker);

    private PostgreSqlContainer? container;

    /// <summary>Gets why the lane cannot run here, or null while Docker answers.</summary>
    public static string? SkipReason => Unreachable.Value;

    /// <inheritdoc />
    public DatabaseProvider Provider => DatabaseProvider.Postgres;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        if (SkipReason is not null)
            return;

        container = new PostgreSqlBuilder().WithImage(PostgresFixture.DefaultImage).Build();
        await container.StartAsync();
    }

    /// <inheritdoc />
    public async Task<FilesTestStore> CreateAsync(string folder)
    {
        var server = container ?? throw new InvalidOperationException($"The PostgreSQL lane did not start: {SkipReason}");
        var name = $"files_{Guid.NewGuid():N}";
        await ExecuteAsync(server, $"CREATE DATABASE {name}");
        var connectionString = new NpgsqlConnectionStringBuilder(server.GetConnectionString()) { Database = name }.ConnectionString;

        // FORCE closes the connections the store's pools still hold, so the server never runs out of them.
        return new FilesTestStore(DatabaseProvider.Postgres, connectionString, async () =>
        {
            NpgsqlConnection.ClearAllPools();
            await ExecuteAsync(server, $"DROP DATABASE IF EXISTS {name} WITH (FORCE)");
        });
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (container is not null)
            await container.DisposeAsync();
    }

    private static async Task ExecuteAsync(PostgreSqlContainer server, string sql)
    {
        await using var connection = new NpgsqlConnection(server.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>Asks Testcontainers for a Docker endpoint that answers; it pings each candidate once per process.</summary>
    private static string? ProbeDocker()
    {
        if (string.Equals(Environment.GetEnvironmentVariable(SwitchVariable), "off", StringComparison.OrdinalIgnoreCase))
            return $"{SwitchVariable}=off, so the PostgreSQL lane is skipped; the SQLite lane ran.";

        try
        {
            return TestcontainersSettings.OS.DockerEndpointAuthConfig is null
                ? "Docker is not reachable, so the PostgreSQL lane is skipped; the SQLite lane ran."
                : null;
        }
        catch (Exception exception)
        {
            return $"Docker is not reachable ({exception.GetType().Name}: {exception.Message}), so the PostgreSQL lane is skipped; the SQLite lane ran.";
        }
    }
}

/// <summary>Shares one <see cref="PostgresLane"/> container across the assembly's PostgreSQL-lane classes.</summary>
[CollectionDefinition(PostgresLane.Collection)]
public sealed class PostgresLaneCollection : ICollectionFixture<PostgresLane>;
