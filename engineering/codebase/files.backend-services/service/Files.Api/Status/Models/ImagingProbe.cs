using SkiaSharp;

namespace Files.Api.Status.Models;

/// <summary>Tells whether this process can encode the WebP variants: SkiaSharp's native library loads, with the system
/// libraries it needs (<c>libfontconfig1</c> in the image). Checked once per process.</summary>
public static class ImagingProbe
{
    private static readonly Lazy<bool> Available = new(Probe);

    /// <summary>Gets <c>ok</c> when a WebP encodes, else <c>unavailable</c>: uploads still succeed, with no variants.</summary>
    public static string State => Available.Value ? "ok" : "unavailable";

    private static bool Probe()
    {
        try
        {
            using var bitmap = new SKBitmap(1, 1);
            using var data = bitmap.Encode(SKEncodedImageFormat.Webp, 80);
            return data is { Size: > 0 };
        }
        catch (Exception exception) when (exception is DllNotFoundException or TypeInitializationException or BadImageFormatException or EntryPointNotFoundException)
        {
            return false;
        }
    }
}
