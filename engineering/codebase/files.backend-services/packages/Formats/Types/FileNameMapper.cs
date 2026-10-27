using System.Globalization;
using System.Text;
using WoW.Two.Sdk.Beta.Be.Files.Urls;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;

namespace WoW.Two.Sdk.Beta.Be.Files.Formats.Types;

/// <summary>Maps the names uploaders give to names safe to store, list and download, and to their parts.</summary>
public static class FileNameMapper
{
    private static readonly string[] DeviceNames =
    [
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    ];

    /// <summary>Maps an uploader's name to a safe one: NFC, the last path part only, no control, format or reserved
    /// characters, single spaces, no trailing dots, no Windows device name, at most
    /// <see cref="FilesConstants.MaxNameLength"/> characters with the extension kept.</summary>
    /// <param name="name">The name as given.</param>
    /// <returns>The safe name; <see cref="FilesErrors.NameInvalid"/> when nothing usable is left.</returns>
    public static Result<string> ToSafeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<string>.Fail(FilesErrors.NameInvalid());

        var normalized = name.Normalize(NormalizationForm.FormC);
        var lastPart = normalized.Split('/', '\\').LastOrDefault(part => part.Trim().Length > 0) ?? string.Empty;

        var builder = new StringBuilder(lastPart.Length);
        foreach (var character in lastPart)
        {
            if (char.IsWhiteSpace(character))
            {
                if (builder.Length > 0 && builder[^1] != ' ')
                    builder.Append(' ');
            }
            else if (!IsUnsafe(character))
            {
                builder.Append(character);
            }
        }

        var safe = builder.ToString().Trim().TrimEnd('.', ' ');
        if (safe.Length == 0)
            return Result<string>.Fail(FilesErrors.NameInvalid());

        var stem = safe.Split('.')[0];
        if (DeviceNames.Contains(stem, StringComparer.OrdinalIgnoreCase))
            safe = "_" + safe;

        return Result<string>.Ok(Truncate(safe));
    }

    /// <summary>Maps a name to its extension: the text after the last dot, lowercase, without the dot.</summary>
    /// <param name="name">The name; any path part is ignored.</param>
    /// <returns>The extension, or an empty string when the name has none.</returns>
    public static string ExtensionOf(string? name)
    {
        if (string.IsNullOrEmpty(name))
            return string.Empty;

        var lastPart = name[(name.LastIndexOfAny(['/', '\\']) + 1)..];
        var dot = lastPart.LastIndexOf('.');
        return dot < 0 || dot == lastPart.Length - 1 ? string.Empty : lastPart[(dot + 1)..].ToLowerInvariant();
    }

    /// <summary>Maps a name without an extension to one with <paramref name="extension"/>, within the length cap.</summary>
    /// <param name="name">A safe name.</param>
    /// <param name="extension">The extension to add, without the dot.</param>
    /// <returns>The name with the extension.</returns>
    public static string WithExtension(string name, string extension) => Truncate($"{name}.{extension}");

    /// <summary>Maps a name to a <c>Content-Disposition</c> value: an ASCII fallback plus the UTF-8 name (RFC 6266).</summary>
    /// <param name="name">A safe name.</param>
    /// <param name="disposition">Whether the browser shows or saves the file.</param>
    /// <returns>The header value.</returns>
    public static string ToContentDisposition(string name, FileDisposition disposition)
    {
        ArgumentNullException.ThrowIfNull(name);

        var type = disposition == FileDisposition.Attachment ? "attachment" : "inline";
        var ascii = new StringBuilder(name.Length);
        foreach (var character in name)
            ascii.Append(character is >= ' ' and <= '~' and not '"' and not '\\' ? character : '_');

        return $"{type}; filename=\"{ascii}\"; filename*=UTF-8''{Uri.EscapeDataString(name)}";
    }

    /// <summary>Tells a character no stored name keeps: controls, invisible format and direction marks, and
    /// characters Windows reserves.</summary>
    private static bool IsUnsafe(char character) =>
        char.IsControl(character)
        || CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.Format
        || character is '<' or '>' or ':' or '"' or '|' or '?' or '*';

    /// <summary>Cuts a name to the length cap, keeping a short extension and whole surrogate pairs.</summary>
    private static string Truncate(string name)
    {
        if (name.Length <= FilesConstants.MaxNameLength)
            return name;

        var extension = ExtensionOf(name);
        var suffix = extension.Length is > 0 and <= 16 ? "." + name[^extension.Length..] : string.Empty;
        var cut = FilesConstants.MaxNameLength - suffix.Length;
        if (char.IsHighSurrogate(name[cut - 1]))
            cut--;

        return name[..cut].TrimEnd('.', ' ') + suffix;
    }
}
