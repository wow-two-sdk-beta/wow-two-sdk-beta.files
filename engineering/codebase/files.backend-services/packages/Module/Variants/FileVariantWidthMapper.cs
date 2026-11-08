using WoW.Two.Sdk.Beta.Be.Files.Formats.Types;
using WoW.Two.Sdk.Beta.Be.Files.Module.Policies;
using WoW.Two.Sdk.Beta.Be.Files.Store;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Variants;

/// <summary>Decides which variants an upload gets and which one a URL serves: never wider than the source, the
/// narrowest at least as wide as asked, else the widest.</summary>
internal static class FileVariantWidthMapper
{
    /// <summary>Holds the widest variant a policy may ask for, the image service's output limit.</summary>
    public const int MaxWidth = 8192;

    /// <summary>Holds the content types a variant is made from: JPEG, PNG, WebP, and a GIF's first frame.</summary>
    private static readonly HashSet<string> SourceTypes = new(StringComparer.Ordinal)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif",
    };

    /// <summary>Tells whether a recognized type is a raster image variants are made from.</summary>
    /// <param name="type">The registry's type of the upload.</param>
    public static bool IsSource(FileTypeModel type) => type.Kind == FileKind.Image && SourceTypes.Contains(type.MimeType);

    /// <summary>Plans the variant widths of a source: the policy's widths no wider than the source, once each, narrowest
    /// first; none upscales.</summary>
    /// <param name="sourceWidth">The source's upright width in pixels.</param>
    /// <param name="widths">The policy's widths.</param>
    /// <returns>The widths to make.</returns>
    public static IReadOnlyList<int> Plan(int sourceWidth, IEnumerable<int> widths) =>
        widths.Where(width => width > 0 && width <= sourceWidth).Distinct().Order().ToArray();

    /// <summary>Selects the variant width a URL serves: the narrowest at least <paramref name="asked"/>, else the widest.</summary>
    /// <param name="available">The file's variant widths, in any order.</param>
    /// <param name="asked">The width the file is shown at.</param>
    /// <returns>The width; none when the file has no variants.</returns>
    public static int? Select(IEnumerable<int> available, int asked)
    {
        int? atLeast = null, widest = null;
        foreach (var width in available)
        {
            if (width >= asked && (atLeast is null || width < atLeast))
                atLeast = width;
            if (widest is null || width > widest)
                widest = width;
        }

        return atLeast ?? widest;
    }

    /// <summary>Tells whether a policy's variant settings are in range: widths 1 to <see cref="MaxWidth"/>, quality 1 to 100.</summary>
    /// <param name="policy">The policy.</param>
    public static bool IsValid(FilePolicyOptions policy) =>
        policy.Variants.All(width => width is > 0 and <= MaxWidth) && policy.VariantQuality is >= 1 and <= 100;
}
