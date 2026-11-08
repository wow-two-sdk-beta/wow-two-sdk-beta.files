using WoW.Two.Sdk.Beta.Be.Files.Store;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Imports;

/// <summary>Represents the answer to a push.</summary>
/// <param name="FileId">The file the key became.</param>
/// <param name="Sha256">The SHA-256 of its bytes, lowercase hex.</param>
/// <param name="Size">Its size in bytes.</param>
/// <param name="Created">Whether this push moved the key; false when it was already there.</param>
public sealed record FileImportPushResultModel(FileId FileId, string Sha256, long Size, bool Created);
