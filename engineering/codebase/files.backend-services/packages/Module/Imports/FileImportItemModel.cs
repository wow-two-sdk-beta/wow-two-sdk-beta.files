using WoW.Two.Sdk.Beta.Be.Files.Store;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Imports;

/// <summary>Represents one moved key in the id map: the file it became.</summary>
/// <param name="Source">The source.</param>
/// <param name="Key">The key in the source.</param>
/// <param name="Reference">The reference attached to the file; none when the push named none.</param>
/// <param name="FileId">The file.</param>
/// <param name="Sha256">The SHA-256 of its bytes, lowercase hex.</param>
/// <param name="Size">Its size in bytes.</param>
public sealed record FileImportItemModel(string Source, string Key, string? Reference, FileId FileId, string Sha256, long Size);
