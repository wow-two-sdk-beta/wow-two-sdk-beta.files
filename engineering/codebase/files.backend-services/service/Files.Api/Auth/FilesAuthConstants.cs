namespace Files.Api.Auth;

/// <summary>Holds the names of the Files API's authentication.</summary>
public static class FilesAuthConstants
{
    /// <summary>Holds the policy scheme that picks the API key or the token scheme for each request.</summary>
    public const string Scheme = "files";

    /// <summary>Holds the header a tenant API key travels in.</summary>
    public const string ApiKeyHeader = "X-Api-Key";

    /// <summary>Holds the marker every Files API key starts with, so a key presented as a bearer token is told from a JWT.</summary>
    public const string ApiKeyMarker = "files_";

    /// <summary>Holds the route Identity's revocation signals arrive at, under each tenant's host.</summary>
    public const string SignalPath = "signals/identity";

    /// <summary>Holds the audience the token scheme is registered with; never accepted, since every trusted issuer the
    /// resolver returns names its own audiences.</summary>
    internal const string UnsetAudience = "urn:wow2:files:audience-per-issuer";
}
