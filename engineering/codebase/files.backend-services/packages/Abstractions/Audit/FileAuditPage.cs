namespace WoW.Two.Sdk.Beta.Be.Files.Audit;

/// <summary>Represents one page of the audit trail.</summary>
/// <param name="Items">The entries on this page, newest first.</param>
/// <param name="NextCursor">The cursor of the next page; none on the last page.</param>
public sealed record FileAuditPage(IReadOnlyList<FileAuditEntryModel> Items, string? NextCursor);
