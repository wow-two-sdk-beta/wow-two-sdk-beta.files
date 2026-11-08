namespace WoW.Two.Sdk.Beta.Be.Files.Module.Imports;

/// <summary>Represents one page of an import's id map, in push order.</summary>
/// <param name="Items">The moved keys on this page.</param>
/// <param name="Next">The cursor of the next page; none on the last page.</param>
public sealed record FileImportMapPage(IReadOnlyList<FileImportItemModel> Items, string? Next);
