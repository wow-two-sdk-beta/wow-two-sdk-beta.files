namespace WoW.Two.Sdk.Beta.Be.Files.Urls;

/// <summary>Represents a signed URL to a private file on the tenant's content host.</summary>
/// <param name="Url">The absolute URL.</param>
/// <param name="ExpiresAtUtc">When the URL stops working (UTC).</param>
public sealed record SignedFileUrl(Uri Url, DateTimeOffset ExpiresAtUtc);
