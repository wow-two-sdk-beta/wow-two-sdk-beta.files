using System.Runtime.CompilerServices;
using Files.Api.Migrate.Input;

namespace Files.Api.Migrate.Sources;

/// <summary>Reads every file under <c>{root}/{path}</c>; a file's key is its path below that folder, <c>/</c>-separated,
/// and its one placeholder is <c>{path}</c>.</summary>
/// <remarks>Hidden files (a leading <c>.</c> on Linux) are skipped: a blob store's partial writes, not its files. A folder
/// that does not exist reads as an empty source, since a product creates its blob folders on its first write; the root
/// itself must exist, so a missing mount is still refused.</remarks>
/// <param name="folder">The folder to walk, already resolved below the root.</param>
public sealed class VolumeFolderSourceReader(string folder) : ISourceReader
{
    /// <summary>Holds the placeholder a file's key fills.</summary>
    public const string PathPlaceholder = "path";

    private static readonly EnumerationOptions Walk = new()
    {
        RecurseSubdirectories = true,
        IgnoreInaccessible = false,
        AttributesToSkip = FileAttributes.Hidden | FileAttributes.System,
    };

    /// <summary>Creates the reader of a request's <c>volume-folder</c> source.</summary>
    /// <param name="request">The parsed request.</param>
    /// <exception cref="MigrateInputException">The root does not exist, or the path leaves it or names a file.</exception>
    public static VolumeFolderSourceReader For(MigrateRequest request)
    {
        if (!Directory.Exists(request.Root))
            throw new MigrateInputException("The root does not exist.");

        var folder = SourcePaths.Resolve(request.Root!, request.Source.Path!, "source.path");
        return File.Exists(folder) ? throw new MigrateInputException("The source path 'root/source.path' is a file, not a folder.") : new VolumeFolderSourceReader(folder);
    }

    /// <summary>Gets whether the folder exists; a missing one reads as an empty source.</summary>
    public bool Exists => Directory.Exists(folder);

    /// <inheritdoc />
    public Task<SourceTotals> CountAsync(CancellationToken cancellationToken)
    {
        long files = 0, bytes = 0;
        foreach (var file in SourceFiles())
        {
            cancellationToken.ThrowIfCancellationRequested();
            files++;
            bytes += new FileInfo(file).Length;
        }

        return Task.FromResult(new SourceTotals(files, bytes));
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<SourceItem> ReadAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var files = SourceFiles()
            .Select(file => (Path: file, Key: Path.GetRelativePath(folder, file).Replace(Path.DirectorySeparatorChar, '/')))
            .OrderBy(file => file.Key, StringComparer.Ordinal)
            .ToList();

        foreach (var (path, key) in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new SourceItem
            {
                Key = key,
                Values = new Dictionary<string, string>(StringComparer.Ordinal) { [PathPlaceholder] = key },
                Size = new FileInfo(path).Length,
                FilePath = path,
                OpenContent = () => new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan),
            };
        }

        await Task.CompletedTask;
    }

    private IEnumerable<string> SourceFiles() => Exists ? Directory.EnumerateFiles(folder, "*", Walk) : [];
}
