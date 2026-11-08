using System.Data.Common;
using Microsoft.Data.Sqlite;
using Npgsql;
using WoW.Two.Sdk.Beta.Be.Data.Migrations.Bespoke;
using WoW.Two.Sdk.Beta.Be.Files.Module;
using WoW.Two.Sdk.Beta.Be.Foundation.Naming;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;
using WoW.Two.Sdk.Beta.Be.Testing.Data.Migrations;

namespace Files.Tests.Lanes;

/// <summary>Makes empty Files stores on one engine: a test lane.</summary>
/// <remarks>Every DB-touching tier runs its suite once per lane: <see cref="SqliteLane"/> in process, with no Docker, and
/// <see cref="PostgresLane"/> in one container per test assembly, skipped when Docker is unreachable.</remarks>
public interface IFilesLane
{
    /// <summary>Gets the lane's engine.</summary>
    DatabaseProvider Provider { get; }

    /// <summary>Creates an empty store, with no schema yet.</summary>
    /// <param name="folder">A folder the test owns, for a store that lives in a file.</param>
    /// <returns>The store; disposing it drops it.</returns>
    Task<FilesTestStore> CreateAsync(string folder);
}

/// <summary>The SQLite lane: a store is a file in the test's folder.</summary>
public sealed class SqliteLane : IFilesLane
{
    /// <summary>Gets the lane.</summary>
    public static SqliteLane Instance { get; } = new();

    /// <inheritdoc />
    public DatabaseProvider Provider => DatabaseProvider.Sqlite;

    /// <inheritdoc />
    public Task<FilesTestStore> CreateAsync(string folder) => Task.FromResult(new FilesTestStore(
        DatabaseProvider.Sqlite,
        $"Data Source={Path.Combine(folder, "files.db")}",
        static () =>
        {
            // Pooled connections hold the file open, so release them before the folder goes.
            SqliteConnection.ClearAllPools();
            return ValueTask.CompletedTask;
        }));
}

/// <summary>One empty Files store of a lane, with the helpers a test reads it through.</summary>
/// <param name="provider">The store's engine.</param>
/// <param name="connectionString">The store's connection string.</param>
/// <param name="drop">Drops the store.</param>
public sealed class FilesTestStore(DatabaseProvider provider, string connectionString, Func<ValueTask> drop) : IAsyncDisposable
{
    /// <summary>Gets the store's engine.</summary>
    public DatabaseProvider Provider => provider;

    /// <summary>Gets the store's connection string.</summary>
    public string ConnectionString => connectionString;

    /// <summary>Points the module at the store.</summary>
    /// <param name="options">The module's settings.</param>
    public void ApplyTo(FilesModuleOptions options)
    {
        options.Provider = provider;
        options.ConnectionString = connectionString;
    }

    /// <summary>Builds a migrator running the module's set for the store's engine.</summary>
    /// <param name="configure">An optional hook over the set's options.</param>
    /// <returns>The migrator; the caller disposes it.</returns>
    public MigratorHarness CreateMigrator(Action<MigrationOptions>? configure = null)
    {
        void Sets(MigratorSetsBuilder sets) => sets.Add(FilesMigrations.SetName, _ => FilesMigrations.CreateBroker(provider), configure);
        return provider == DatabaseProvider.Sqlite
            ? MigratorHarness.CreateSqlite(connectionString, Sets)
            : MigratorHarness.CreatePostgres(connectionString, Sets);
    }

    /// <summary>Applies the module's set to the store.</summary>
    public async Task MigrateAsync()
    {
        await using var migrator = CreateMigrator();
        (await migrator.ApplyAllAsync()).ValueOrThrow();
    }

    /// <summary>Names an instant's column as the store spells it: <c>{stem}</c> on PostgreSQL, <c>{stem}_utc</c> on SQLite.</summary>
    /// <param name="stem">The column's name ending in <c>_at</c>.</param>
    /// <returns>The column's name.</returns>
    public string At(string stem) => provider == DatabaseProvider.Postgres ? stem : stem + "_utc";

    /// <summary>Spells an enum value as the store holds it: a snake_case label on PostgreSQL, the member's name on SQLite.</summary>
    /// <typeparam name="TEnum">The enum.</typeparam>
    /// <param name="value">The value.</param>
    /// <returns>The stored text.</returns>
    public string Stored<TEnum>(TEnum value) where TEnum : struct, Enum =>
        provider == DatabaseProvider.Postgres ? CaseMapper.ToCase(value.ToString(), CaseStyle.Snake) : value.ToString();

    /// <summary>Opens a connection to the store.</summary>
    /// <returns>The open connection; the caller disposes it.</returns>
    public async Task<DbConnection> OpenAsync()
    {
        DbConnection connection = provider == DatabaseProvider.Sqlite
            ? new SqliteConnection(connectionString)
            : new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        return connection;
    }

    /// <summary>Reads rows straight from the store; each row maps column names to values. Integers read as
    /// <see cref="long"/> and uuids as lowercase text on both engines, as SQLite stores them.</summary>
    /// <param name="sql">The query.</param>
    /// <returns>The rows.</returns>
    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> RowsAsync(string sql)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<IReadOnlyDictionary<string, object?>>();
        while (await reader.ReadAsync())
            rows.Add(Enumerable.Range(0, reader.FieldCount).ToDictionary(i => reader.GetName(i), i => Normalize(reader.IsDBNull(i) ? null : reader.GetValue(i))));
        return rows;
    }

    /// <summary>Lists a table's columns in their order.</summary>
    /// <param name="table">The table.</param>
    /// <returns>The column names.</returns>
    public async Task<IReadOnlyList<string>> ColumnsAsync(string table)
    {
        var sql = provider == DatabaseProvider.Sqlite
            ? $"SELECT name FROM pragma_table_info('{table}')"
            : $"SELECT column_name FROM information_schema.columns WHERE table_schema = current_schema() AND table_name = '{table}' ORDER BY ordinal_position";
        return (await RowsAsync(sql)).Select(row => (string)row.Values.Single()!).ToList();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => drop();

    private static object? Normalize(object? value) => value switch
    {
        int number => (long)number,
        short number => (long)number,
        Guid uuid => uuid.ToString("D"),
        _ => value,
    };
}
