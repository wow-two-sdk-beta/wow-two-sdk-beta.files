namespace WoW.Two.Sdk.Beta.Be.Files.Module.Imports;

/// <summary>Defines the import token: Files' own credential for one import, never an Identity token.</summary>
public interface IFileImportTokenService
{
    /// <summary>Issues a token for an import, living <see cref="FileImportOptions.TokenLifetime"/>.</summary>
    /// <param name="importId">The import.</param>
    /// <param name="tenantId">The import's tenant.</param>
    /// <returns>The token and its expiry.</returns>
    FileImportTokenModel Issue(string importId, string tenantId);

    /// <summary>Reads a presented token.</summary>
    /// <param name="token">The token as presented.</param>
    /// <returns>What it says; none when it is malformed, its mac is bad or it has expired.</returns>
    FileImportTokenClaims? Read(string? token);
}
