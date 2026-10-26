namespace WoW.Two.Sdk.Beta.Be.Files.Store;

/// <summary>Filters and pages a file list; every filter is optional.</summary>
public sealed record FileQuery
{
    /// <summary>Gets the app whose files to list.</summary>
    public string? AppId { get; init; }

    /// <summary>Gets the subject whose files to list.</summary>
    public string? OwnerId { get; init; }

    /// <summary>Gets the kind to list.</summary>
    public FileKind? Kind { get; init; }

    /// <summary>Gets the state to list.</summary>
    public FileState? State { get; init; }

    /// <summary>Gets the earliest creation time to list (UTC).</summary>
    public DateTimeOffset? CreatedAfterUtc { get; init; }

    /// <summary>Gets whether deleted, still restorable files are listed too.</summary>
    public bool IncludeDeleted { get; init; }

    /// <summary>Gets the cursor a previous page returned.</summary>
    public string? Cursor { get; init; }

    /// <summary>Gets the page size, at most <see cref="FilesConstants.MaxPageSize"/>.</summary>
    public int Limit { get; init; } = 50;
}
