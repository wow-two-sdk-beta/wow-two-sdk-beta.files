namespace Files.Api.Auth;

/// <summary>Holds who may call the Files API: trusted token issuers, each bound to a tenant, and tenant API keys.</summary>
/// <remarks>Bound from <c>Files:Auth</c> and read on use, so a test host's late configuration reaches it. Issuers and keys
/// move to the tenants table with the tenant admin API (t8 C8, C9, C14).</remarks>
public sealed class FilesAuthSettings
{
    /// <summary>Holds the section the settings bind from.</summary>
    public const string SectionName = "Files:Auth";

    /// <summary>Gets or sets this instance's own resource URI, with its trailing slash: the audience of an issuer that
    /// names none, such as an A's only issuer or a tenant on a shared host's default host.</summary>
    public string? Audience { get; set; }

    /// <summary>Gets the issuers whose tokens the API accepts.</summary>
    public List<FilesIssuerSettings> Issuers { get; } = [];

    /// <summary>Gets the API keys the API accepts: callers outside Identity only.</summary>
    public List<FilesApiKeySettings> ApiKeys { get; } = [];
}

/// <summary>Holds one trusted issuer: the tenant its callers act in, the audiences its tokens may name, and where its
/// keys come from.</summary>
public sealed class FilesIssuerSettings
{
    /// <summary>Gets or sets the issuer, compared exactly with a token's <c>iss</c>.</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Gets or sets the tenant the issuer's callers act in, stamped as the principal's partition.</summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>Gets the audiences the issuer's tokens may name: the tenant's own resource URIs, each with its trailing
    /// slash; empty means <see cref="FilesAuthSettings.Audience"/>.</summary>
    public List<string> Audiences { get; } = [];

    /// <summary>Gets or sets the discovery address; null means <c>{Issuer}/.well-known/openid-configuration</c>.</summary>
    public Uri? MetadataAddress { get; set; }

    /// <summary>Gets or sets a JSON Web Key Set of the issuer's public keys, used instead of discovery: an issuer this host
    /// can't reach, or a test host.</summary>
    public string? Jwks { get; set; }
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

    /// <summary>Gets or sets the organization the key is bound to; null leaves it unbound.</summary>
    public string? OrganizationId { get; set; }

    /// <summary>Gets or sets when the key stops working; null never.</summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>Gets the scopes the key carries, such as <c>files.read</c>.</summary>
    public List<string> Scopes { get; } = [];
}
