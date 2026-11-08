using System.Data.Common;
using Files.Api.Migrate.Input;
using Microsoft.Data.Sqlite;

namespace Files.Api.Migrate.Sources;

/// <summary>Reads a <c>sqlite-column</c> source: <c>{root}/{database}</c>, opened with <c>Mode=ReadOnly</c>.</summary>
/// <param name="database">The database file, already resolved below the root.</param>
/// <param name="query">The source's query.</param>
public sealed class SqliteColumnSourceReader(string database, ColumnQuery query) : ColumnSourceReader(query)
{
    /// <summary>Creates the reader of a request's <c>sqlite-column</c> source.</summary>
    /// <param name="request">The parsed request.</param>
    /// <exception cref="MigrateInputException">The database leaves the root or does not exist.</exception>
    public static SqliteColumnSourceReader For(MigrateRequest request)
    {
        var database = SourcePaths.Resolve(request.Root!, request.Source.Database!, "source.database");
        return File.Exists(database)
            ? new SqliteColumnSourceReader(database, ColumnQuery.ForSqlite(request.Source))
            : throw new MigrateInputException("The source database 'root/source.database' does not exist.");
    }

    /// <inheritdoc />
    protected override async Task<DbConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = database,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false,
        }.ToString());
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
