using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace Files.Api.Migrate.Sources;

/// <summary>Reads a column source over any ADO.NET connection: one select in key order, rows mapped by
/// <see cref="ColumnQuery"/>.</summary>
/// <remarks>Commands run without a timeout: a large table streams for as long as the uploads take.</remarks>
/// <param name="query">The source's query.</param>
public abstract class ColumnSourceReader(ColumnQuery query) : ISourceReader
{
    /// <summary>Gets the source's query.</summary>
    public ColumnQuery Query => query;

    /// <summary>Gets the behavior the select runs with.</summary>
    protected virtual CommandBehavior ReadBehavior => CommandBehavior.Default;

    /// <inheritdoc />
    public async Task<SourceTotals> CountAsync(CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = Command(connection, query.CountSql);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return new SourceTotals(0, 0);

        return new SourceTotals(Convert.ToInt64(reader.GetValue(0)), reader.IsDBNull(1) ? 0 : Convert.ToInt64(reader.GetValue(1)));
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<SourceItem> ReadAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = Command(connection, query.SelectSql);
        await using var reader = await command.ExecuteReaderAsync(ReadBehavior, cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            yield return query.Map(reader);
    }

    /// <summary>Opens a connection to the source, read-only.</summary>
    /// <param name="cancellationToken">The token that cancels the open.</param>
    protected abstract Task<DbConnection> OpenAsync(CancellationToken cancellationToken);

    private static DbCommand Command(DbConnection connection, string sql)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 0;
        return command;
    }
}
