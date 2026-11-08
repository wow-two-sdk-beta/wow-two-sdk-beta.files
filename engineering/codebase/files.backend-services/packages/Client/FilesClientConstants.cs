namespace WoW.Two.Sdk.Beta.Be.Files.Client;

/// <summary>Holds the names the Files HTTP client registers under.</summary>
public static class FilesClientConstants
{
    /// <summary>Holds the named <see cref="HttpClient"/> every Files call ends in: its handlers and primary handler serve every
    /// call, and a call with no Identity token goes through it directly.</summary>
    public const string HttpClientName = "files";

    /// <summary>Holds the named <see cref="HttpClient"/> that adds the service's own client-credentials token, then hands the
    /// call to <see cref="HttpClientName"/>.</summary>
    public const string ServiceHttpClientName = "files-service";

    /// <summary>Holds the named <see cref="HttpClient"/> that adds the person's exchanged token, then hands the call to
    /// <see cref="HttpClientName"/>.</summary>
    public const string PersonHttpClientName = "files-person";

    /// <summary>Holds the header a tenant API key travels in.</summary>
    public const string ApiKeyHeader = "X-Api-Key";
}
