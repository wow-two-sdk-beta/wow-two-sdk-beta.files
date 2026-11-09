using System.Globalization;
using Files.Api.Migrate.Api;

namespace Files.Api.Migrate.Sources;

/// <summary>Copies a closed WAL database — no <c>-wal</c> beside it, every page in the main file — to a private file
/// the mover alone reads.</summary>
/// <remarks>A product that still runs can close its last pooled connection and drop its <c>-wal</c>; its next write and
/// close checkpoint into the main file. The copy holds only while the main file's size and last write time stay the
/// same across it, so a changed one is copied again, at most <see cref="Attempts"/> times.</remarks>
public static class SqliteSnapshot
{
    /// <summary>The copies tried before the run fails.</summary>
    public const int Attempts = 3;

    /// <summary>Copies the database into a new file in a folder.</summary>
    /// <param name="database">The database file.</param>
    /// <param name="folder">The folder the copy lands in: the mover's temp folder, never beside the source.</param>
    /// <param name="copy">Copies a file over another; tests inject a writer here.</param>
    /// <returns>The copy's path; the caller deletes it.</returns>
    /// <exception cref="MigrateUnavailableException">The database changed during every copy; a rerun copies again.</exception>
    public static string Take(string database, string folder, Action<string, string>? copy = null)
    {
        copy ??= static (from, to) => File.Copy(from, to, overwrite: true);
        var snapshot = Path.Combine(folder, $"files-mover-{Guid.NewGuid():N}.db");
        try
        {
            for (var attempt = 0; attempt < Attempts; attempt++)
            {
                var before = Stamp(database);
                copy(database, snapshot);
                if (Stamp(database) == before)
                    return snapshot;
            }
        }
        catch
        {
            Delete(snapshot);
            throw;
        }

        Delete(snapshot);
        throw new MigrateUnavailableException(string.Create(CultureInfo.InvariantCulture,
            $"The source database changed during each of {Attempts} copies; a rerun copies it again."));
    }

    /// <summary>Deletes a copy; a missing one is fine.</summary>
    /// <param name="snapshot">The copy's path.</param>
    public static void Delete(string snapshot)
    {
        try
        {
            File.Delete(snapshot);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // A copy left in the temp folder goes with the container.
        }
    }

    private static (long Length, DateTime Written) Stamp(string database)
    {
        var file = new FileInfo(database);
        return (file.Length, file.LastWriteTimeUtc);
    }
}
