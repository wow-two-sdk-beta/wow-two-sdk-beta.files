namespace WoW.Two.Sdk.Beta.Be.Files.Urls;

/// <summary>Describes a signed URL to issue.</summary>
public sealed record UrlSpec
{
    /// <summary>Gets how long the URL stays valid; the instance caps it.</summary>
    public TimeSpan Lifetime { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>Gets whether the browser shows or saves the file.</summary>
    public FileDisposition Disposition { get; init; } = FileDisposition.Inline;

    /// <summary>Gets the variant to serve, by preset name; the original when none.</summary>
    public string? Variant { get; init; }
}
