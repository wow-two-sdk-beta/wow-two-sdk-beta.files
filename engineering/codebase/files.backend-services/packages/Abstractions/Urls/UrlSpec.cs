namespace WoW.Two.Sdk.Beta.Be.Files.Urls;

/// <summary>Describes a signed URL to issue.</summary>
public sealed record UrlSpec
{
    /// <summary>Gets how long the URL stays valid; the instance caps it.</summary>
    public TimeSpan Lifetime { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>Gets whether the browser shows or saves the file.</summary>
    public FileDisposition Disposition { get; init; } = FileDisposition.Inline;

    /// <summary>Gets the width in pixels the file is shown at: the narrowest variant at least that wide serves, else the
    /// widest; the original when none is asked or the file has no variants. Must be positive when set.</summary>
    public int? Width { get; init; }
}
