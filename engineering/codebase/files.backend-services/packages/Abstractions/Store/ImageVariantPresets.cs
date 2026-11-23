namespace WoW.Two.Sdk.Beta.Be.Files.Store;

/// <summary>Maps shared image sizes to their target widths in pixels.</summary>
public static class ImageVariantPresets
{
    /// <summary>Looks up one of the five case-sensitive preset names.</summary>
    /// <param name="name">The preset name, from <c>s1</c> through <c>s5</c>.</param>
    /// <param name="width">The target width, or zero for an unknown name.</param>
    /// <returns>Whether the name is known.</returns>
    public static bool TryGetWidth(string? name, out int width)
    {
        width = name switch
        {
            "s1" => 160,
            "s2" => 480,
            "s3" => 640,
            "s4" => 900,
            "s5" => 1400,
            _ => 0,
        };
        return width > 0;
    }
}
