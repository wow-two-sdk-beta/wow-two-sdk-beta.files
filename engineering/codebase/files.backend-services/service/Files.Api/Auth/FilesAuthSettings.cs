namespace Files.Api.Auth;

/// <summary>Holds who may call the Files API: trusted token issuers, each bound to a tenant, and tenant API keys.</summary>
/// <remarks>Bound from <c>Files:Auth</c>. Keys and issuers move to the tenants table with the admin API.</remarks>
public sealed class FilesAuthSettings
{
    /// <summary>Holds the section the settings bind from.</summary>
    public const string SectionName = "Files:Auth";

    /// <summary>Gets the issuers whose client-credentials tokens the API accepts.</summary>
    public List<FilesIssuerSettings> Issuers { get; } = [];

    /// <summary>Gets the API keys the API accepts.</summary>
    public List<FilesApiKeySettings> ApiKeys { get; } = [];
}

/// <summary>Holds one trusted issuer: where its keys come from, the audience its tokens name, and the tenant they act in.</summary>
public sealed class FilesIssuerSettings
{
    /// <summary>Gets or sets the issuer, the tokens' <c>iss</c>.</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Gets or sets the audience the tokens must name: this instance's resource URI.</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>Gets or sets the OIDC authority the signing keys are discovered from; the issuer when empty.</summary>
    public string? Authority { get; set; }

    /// <summary>Gets or sets a symmetric signing key instead of discovery — local runs and tests only.</summary>
    public string? SigningKey { get; set; }

    /// <summary>Gets or sets the tenant the issuer's callers act in.</summary>
    public string TenantId { get; set; } = string.Empty;
}

/// <summary>Holds one tenant API key: its SHA-256, never the key itself.</summary>
public sealed class FilesApiKeySettings
{
    /// <summary>Gets or sets the key's id, recorded as the caller's subject.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the SHA-256 of the key, lowercase hex.</summary>
    public string Sha256 { get; set; } = string.Empty;

    /// <summary>Gets or sets the tenant the key acts in.</summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>Gets or sets the app the key belongs to, whose upload policy applies.</summary>
    public string? AppId { get; set; }

    /// <summary>Gets the scopes the key carries, such as <c>files.read</c>.</summary>
    public List<string> Scopes { get; } = [];
}
