using System.Data;
using System.Data.Common;
using Files.Api.Migrate.Input;
using Npgsql;

namespace Files.Api.Migrate.Sources;

/// <summary>Reads a <c>postgres-column</c> source over the request's read-only connection, streaming each
/// <c>bytea</c> with <see cref="CommandBehavior.SequentialAccess"/> so no row is held whole in memory.</summary>
/// <param name="connection">The PostgreSQL connection string.</param>
/// <param name="query">The source's query.</param>
public sealed class PostgresColumnSourceReader(string connection, ColumnQuery query) : ColumnSourceReader(query)
{
    /// <summary>Creates the reader of a request's <c>postgres-column</c> source.</summary>
    /// <param name="request">The parsed request.</param>
    /// <exception cref="MigrateInputException">The connection string does not parse.</exception>
    public static PostgresColumnSourceReader For(MigrateRequest request)
    {
        try
        {
            _ = new NpgsqlConnectionStringBuilder(request.Connection);
        }
        catch (ArgumentException)
        {
            throw new MigrateInputException("The field 'connection' is no PostgreSQL connection string.");
        }

        return new PostgresColumnSourceReader(request.Connection!, ColumnQuery.ForPostgres(request.Source));
    }

    /// <inheritdoc />
    protected override CommandBehavior ReadBehavior => CommandBehavior.SequentialAccess;

    /// <inheritdoc />
    protected override async Task<DbConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var npgsql = new NpgsqlConnection(connection);
        await npgsql.OpenAsync(cancellationToken);
        return npgsql;
    }
}
