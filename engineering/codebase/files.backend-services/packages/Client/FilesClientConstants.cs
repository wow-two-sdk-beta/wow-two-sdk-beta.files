namespace WoW.Two.Sdk.Beta.Be.Files.Client;

/// <summary>Holds the names the Files HTTP client registers under.</summary>
public static class FilesClientConstants
{
    /// <summary>Holds the named <see cref="HttpClient"/> every Files broker calls through.</summary>
    public const string HttpClientName = "files";

    /// <summary>Holds the header a tenant API key travels in.</summary>
    public const string ApiKeyHeader = "X-Api-Key";
}
