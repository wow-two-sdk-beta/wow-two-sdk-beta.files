namespace Files.Api.Auth;

/// <summary>Holds the authentication scheme names of the Files API.</summary>
public static class FilesAuthConstants
{
    /// <summary>Holds the policy scheme that picks the API key or the token scheme for each request.</summary>
    public const string Scheme = "files";

    /// <summary>Holds the tenant API key scheme.</summary>
    public const string ApiKeyScheme = "files-api-key";

    /// <summary>Holds the bearer scheme that accepts the trusted issuers' tokens.</summary>
    public const string TokenScheme = "files-token";

    /// <summary>Holds the header a tenant API key travels in.</summary>
    public const string ApiKeyHeader = "X-Api-Key";
}
