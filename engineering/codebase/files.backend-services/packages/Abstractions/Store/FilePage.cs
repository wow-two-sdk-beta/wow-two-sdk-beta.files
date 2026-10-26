namespace WoW.Two.Sdk.Beta.Be.Files.Store;

/// <summary>Represents one page of a file list.</summary>
/// <param name="Items">The files on this page, newest first.</param>
/// <param name="NextCursor">The cursor of the next page; none on the last page.</param>
public sealed record FilePage(IReadOnlyList<FileModel> Items, string? NextCursor);
