namespace WoW.Two.Sdk.Beta.Be.Files.Store;

/// <summary>Represents one image variant of a file: a resized copy made on upload at one of the app policy's widths.</summary>
/// <remarks>A variant goes with its source: it is served while the source is, and deleted, restored and purged with it.</remarks>
public sealed record FileVariantModel
{
    /// <summary>Gets the width in pixels, one of the app policy's widths.</summary>
    public required int Width { get; init; }

    /// <summary>Gets the height in pixels, keeping the source's aspect ratio.</summary>
    public required int Height { get; init; }

    /// <summary>Gets the content type of the variant's bytes, such as <c>image/webp</c>.</summary>
    public required string ContentType { get; init; }

    /// <summary>Gets the size in bytes.</summary>
    public required long Size { get; init; }
}
