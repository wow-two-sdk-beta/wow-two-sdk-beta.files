using System.Globalization;
using System.Text.Json;
using WoW.Two.Sdk.Beta.Be.Files.Store;

namespace WoW.Two.Sdk.Beta.Be.Files.Formats.Types;

/// <summary>Parses the registry's JSON (<c>Registry/file-types.json</c>) into file types.</summary>
internal static class FileTypeTableParser
{
    /// <summary>Parses the registry document.</summary>
    /// <param name="json">The registry document.</param>
    /// <returns>The rows, in file order.</returns>
    /// <exception cref="FormatException">A row is malformed.</exception>
    public static IReadOnlyList<FileTypeModel> Parse(Stream json)
    {
        var document = JsonSerializer.Deserialize<TableDocument>(json, JsonSerializerOptions.Web)
            ?? throw new FormatException("The file-type registry is empty.");

        return document.Types.Select(ParseRow).ToList();
    }

    private static FileTypeModel ParseRow(TableRow row)
    {
        if (string.IsNullOrWhiteSpace(row.Ext) || string.IsNullOrWhiteSpace(row.Mime) || string.IsNullOrWhiteSpace(row.Label))
            throw new FormatException($"The file-type row '{row.Ext}' needs ext, mime and label.");

        return new FileTypeModel
        {
            Extension = row.Ext,
            MimeType = row.Mime,
            Kind = ParseEnum<FileKind>(row.Kind, row.Ext),
            Label = row.Label,
            Signatures = (row.Magic ?? []).Select(signature => ParseSignature(signature, row.Ext)).ToList(),
            Preview = ParseEnum<FilePreview>(row.Preview ?? "none", row.Ext),
            Risk = ParseEnum<FileRisk>(row.Risk ?? "none", row.Ext),
        };
    }

    /// <summary>Parses <c>"offset:hex offset:hex"</c>; a leading <c>~</c> marks a signature that never detects alone.</summary>
    private static FileSignatureModel ParseSignature(string text, string ext)
    {
        var detects = !text.StartsWith('~');
        var parts = text.TrimStart('~').Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(part =>
        {
            var colon = part.IndexOf(':');
            if (colon <= 0 || !int.TryParse(part.AsSpan(0, colon), NumberStyles.None, CultureInfo.InvariantCulture, out var offset))
                throw new FormatException($"The signature '{text}' of '{ext}' needs offset:hex parts.");

            return new FileSignaturePart(offset, Convert.FromHexString(part.AsSpan(colon + 1)));
        }).ToList();

        return parts.Count > 0
            ? new FileSignatureModel { Parts = parts, Detects = detects }
            : throw new FormatException($"The signature of '{ext}' is empty.");
    }

    /// <summary>Parses a kebab-case value (<c>raw-photo</c>) into its enum member (<c>RawPhoto</c>).</summary>
    private static TEnum ParseEnum<TEnum>(string? value, string ext) where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(value?.Replace("-", string.Empty, StringComparison.Ordinal), ignoreCase: true, out var parsed)
        && Enum.IsDefined(parsed)
            ? parsed
            : throw new FormatException($"The value '{value}' of '{ext}' is not a {typeof(TEnum).Name}.");

    private sealed record TableDocument(int Version, IReadOnlyList<TableRow> Types);

    private sealed record TableRow(string Ext, string Mime, string? Kind, string Label, IReadOnlyList<string>? Magic, string? Preview, string? Risk);
}
