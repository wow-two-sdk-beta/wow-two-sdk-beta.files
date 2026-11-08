namespace Files.Api.Migrate.Sources;

/// <summary>Reads one source kind's items: their keys, placeholder values and bytes.</summary>
public interface ISourceReader
{
    /// <summary>Counts the source's files and bytes without reading their bytes; <c>plan</c> calls it.</summary>
    /// <param name="cancellationToken">The token that cancels the count.</param>
    Task<SourceTotals> CountAsync(CancellationToken cancellationToken);

    /// <summary>Reads every item in key order.</summary>
    /// <remarks>An item's <see cref="SourceItem.OpenContent"/> is valid only until the next item is read, and opens once
    /// unless <see cref="SourceItem.FilePath"/> is set.</remarks>
    /// <param name="cancellationToken">The token that cancels the read.</param>
    IAsyncEnumerable<SourceItem> ReadAsync(CancellationToken cancellationToken);
}

/// <summary>Represents a source's totals.</summary>
/// <param name="Files">The number of files.</param>
/// <param name="Bytes">The sum of their sizes.</param>
public sealed record SourceTotals(long Files, long Bytes);

/// <summary>Represents one source item: a file or a row.</summary>
public sealed class SourceItem
{
    /// <summary>Gets the source key: the path below the folder, or the key columns' values joined with <c>/</c>.</summary>
    public required string Key { get; init; }

    /// <summary>Gets the placeholder values the reference template reads: <c>path</c>, or each key column.</summary>
    public required IReadOnlyDictionary<string, string> Values { get; init; }

    /// <summary>Gets the file's name from the source's <c>name</c> column; <see langword="null"/> lets Files default it.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the file's content type from the <c>type</c> column; <see langword="null"/> lets Files detect it.</summary>
    public string? ContentType { get; init; }

    /// <summary>Gets the size when the source knows it without reading the bytes.</summary>
    public long? Size { get; init; }

    /// <summary>Gets the file the bytes can be reopened from for every upload attempt; <see langword="null"/> for a row,
    /// whose bytes stream once.</summary>
    public string? FilePath { get; init; }

    /// <summary>Gets the function that opens the item's bytes; the caller disposes the stream.</summary>
    public required Func<Stream> OpenContent { get; init; }
}
