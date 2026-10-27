using WoW.Two.Sdk.Beta.Be.Files.Store;

namespace WoW.Two.Sdk.Beta.Be.Files.Audit;

/// <summary>Filters and pages the audit trail; every filter is optional.</summary>
public sealed record FileAuditQuery
{
    /// <summary>Gets the file whose entries to list.</summary>
    public FileId? FileId { get; init; }

    /// <summary>Gets the action to list.</summary>
    public FileAuditAction? Action { get; init; }

    /// <summary>Gets the cursor a previous page returned.</summary>
    public string? Cursor { get; init; }

    /// <summary>Gets the page size, at most <see cref="FilesConstants.MaxPageSize"/>.</summary>
    public int Limit { get; init; } = 50;
}
