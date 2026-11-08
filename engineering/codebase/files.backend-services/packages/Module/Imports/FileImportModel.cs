namespace WoW.Two.Sdk.Beta.Be.Files.Module.Imports;

/// <summary>Represents an import and the progress of each source.</summary>
/// <param name="Id">The import's id.</param>
/// <param name="Tenant">The tenant its files land in.</param>
/// <param name="State">The state; <see cref="FileImportConstants.OpenState"/> in P1.</param>
/// <param name="Sources">Each source's counts, by id.</param>
public sealed record FileImportModel(string Id, string Tenant, string State, IReadOnlyList<FileImportSourceModel> Sources);
