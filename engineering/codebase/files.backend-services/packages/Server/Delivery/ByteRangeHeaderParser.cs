using Microsoft.Net.Http.Headers;
using WoW.Two.Sdk.Beta.Be.Files.Store;

namespace WoW.Two.Sdk.Beta.Be.Files.Server.Delivery;

/// <summary>Parses a request's <c>Range</c> header against a file's size: one byte range is served, several or none
/// serve the whole file, and a range outside the file is unsatisfiable.</summary>
internal static class ByteRangeHeaderParser
{
    /// <summary>Parses the header.</summary>
    /// <param name="header">The <c>Range</c> header, when sent.</param>
    /// <param name="size">The file's size in bytes.</param>
    /// <param name="range">The range to serve; none serves the whole file.</param>
    /// <returns>Whether the request can be served; false answers <c>416</c>.</returns>
    public static bool TryParse(string? header, long size, out ByteRange? range)
    {
        range = null;
        if (string.IsNullOrEmpty(header) || !RangeHeaderValue.TryParse(header, out var parsed)
            || !string.Equals(parsed.Unit.Value, "bytes", StringComparison.OrdinalIgnoreCase) || parsed.Ranges.Count != 1)
            return true;

        var item = parsed.Ranges.First();
        if (item.From is { } from)
        {
            if (from >= size)
                return false;
            range = new ByteRange(from, Math.Min(item.To ?? size - 1, size - 1));
            return true;
        }

        if (item.To is not { } suffix || suffix <= 0 || size == 0)
            return false;
        range = new ByteRange(Math.Max(0, size - suffix), size - 1);
        return true;
    }
}
