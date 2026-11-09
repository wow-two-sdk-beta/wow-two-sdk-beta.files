using System.Data.Common;
using Files.Api.Migrate.Input;
using Microsoft.Data.Sqlite;

namespace Files.Api.Migrate.Sources;

/// <summary>Reads a <c>sqlite-column</c> source: <c>{root}/{database}</c>, opened with <c>Mode=ReadOnly</c>.</summary>
/// <remarks>A WAL database whose <c>-wal</c> file is gone (its product stopped, every page checkpointed) opens
/// <c>immutable</c>: a read-only open would otherwise create the <c>-wal</c> and <c>-shm</c> files, which a read-only
/// mount refuses with SQLite error 14. A live one, its <c>-wal</c> present, reads through it as usual.</remarks>
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
            DataSource = IsClosedWal(database) ? new Uri(database).AbsoluteUri + "?immutable=1" : database,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false,
        }.ToString());
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    /// <summary>Tells a WAL database with no <c>-wal</c> file beside it: its header's format versions (bytes 18 and 19)
    /// read <c>2</c>, and every page sits in the main file.</summary>
    /// <param name="database">The database file.</param>
    public static bool IsClosedWal(string database)
    {
        if (File.Exists(database + "-wal"))
            return false;

        Span<byte> header = stackalloc byte[20];
        using var file = new FileStream(database, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        return file.ReadAtLeast(header, header.Length, throwOnEndOfStream: false) == header.Length && header[18] == 2 && header[19] == 2;
    }
}
