namespace WoW.Two.Sdk.Beta.Be.Files.Client;

/// <summary>Holds the settings of the Files HTTP client: a tenant API key or Identity tokens, at most one of them; with
/// neither, calls go out bare for the caller's own handler.</summary>
public sealed class FilesClientOptions
{
    /// <summary>Gets or sets the absolute root URL of the running Files service.</summary>
    public Uri? BaseAddress { get; set; }

    /// <summary>Gets or sets the tenant API key sent with every call; none when the client sends tokens instead.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Gets or sets the Identity client the Files client gets its tokens from: the service's own client-credentials
    /// token, or the person's token exchanged for one Files accepts; none when the client sends an API key instead.</summary>
    public FilesClientIdentityOptions? Identity { get; set; }

    /// <summary>Returns the service's root URL with its trailing slash, as calls resolve against it.</summary>
    internal static string Root(Uri baseAddress)
    {
        var root = baseAddress.ToString();
        return root.EndsWith('/') ? root : root + "/";
    }
}
