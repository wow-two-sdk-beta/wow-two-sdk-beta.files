namespace WoW.Two.Sdk.Beta.Be.Files.Module.Urls;

/// <summary>Holds where files are served and the key that signs private file URLs.</summary>
/// <remarks>Issuing a URL needs both <see cref="BaseUrl"/> and <see cref="SigningKey"/>; a host that never issues one
/// sets neither.</remarks>
public sealed class FileDeliveryOptions
{
    /// <summary>Gets or sets the content host's root, such as <c>https://files.example.com</c>, never an app's origin.</summary>
    public Uri? BaseUrl { get; set; }

    /// <summary>Gets or sets the HMAC key of signed URLs, at least 32 characters; rotating it voids every issued URL.</summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the longest lifetime a signed URL gets. Default 7 days.</summary>
    public TimeSpan MaxLifetime { get; set; } = TimeSpan.FromDays(7);
}
