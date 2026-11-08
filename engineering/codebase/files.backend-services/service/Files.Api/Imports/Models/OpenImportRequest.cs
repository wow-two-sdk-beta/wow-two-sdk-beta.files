namespace Files.Api.Imports.Models;

/// <summary>Represents the body that opens an import.</summary>
/// <param name="Tenant">The tenant the files land in.</param>
/// <param name="Sources">The sources they come from.</param>
public sealed record OpenImportRequest(string? Tenant, IReadOnlyList<string>? Sources);
