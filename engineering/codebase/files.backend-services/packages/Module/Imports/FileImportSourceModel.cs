namespace WoW.Two.Sdk.Beta.Be.Files.Module.Imports;

/// <summary>Represents one source's progress.</summary>
/// <param name="Id">The source's id.</param>
/// <param name="Files">How many keys moved.</param>
/// <param name="Bytes">How many bytes they hold.</param>
/// <param name="Failed">How many pushes were refused.</param>
public sealed record FileImportSourceModel(string Id, long Files, long Bytes, long Failed);
