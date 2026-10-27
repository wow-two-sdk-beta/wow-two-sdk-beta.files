namespace WoW.Two.Sdk.Beta.Be.Files.Client;

/// <summary>Holds the settings of the Files HTTP client.</summary>
public sealed class FilesClientOptions
{
    /// <summary>Gets or sets the absolute root URL of the running Files service.</summary>
    public Uri? BaseAddress { get; set; }

    /// <summary>Gets or sets the tenant API key sent with every call; none when the client sends tokens instead.</summary>
    public string? ApiKey { get; set; }
}
