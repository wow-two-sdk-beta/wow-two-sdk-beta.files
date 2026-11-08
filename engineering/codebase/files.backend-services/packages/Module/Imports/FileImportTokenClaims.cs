namespace WoW.Two.Sdk.Beta.Be.Files.Module.Imports;

/// <summary>Represents what a valid import token says.</summary>
/// <param name="ImportId">The import it reaches.</param>
/// <param name="TenantId">The import's tenant.</param>
/// <param name="ExpiresAt">When it stops working.</param>
public sealed record FileImportTokenClaims(string ImportId, string TenantId, DateTimeOffset ExpiresAt);
