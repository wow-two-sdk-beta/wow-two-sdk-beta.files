namespace WoW.Two.Sdk.Beta.Be.Files.Module.Callers;

/// <summary>Holds the claims a Files host stamps on its callers.</summary>
public static class FilesCallerClaimConstants
{
    /// <summary>Holds the claim naming the tenant a caller acts in, stamped from the API key or the token's issuer.</summary>
    public const string Tenant = "files_tenant";
}
