using System.Reflection;

namespace WoW.Two.Sdk.Beta.Be.Files.Formats.Types;

/// <summary>Maps extensions, file names, content types and leading bytes to file types, over the registry's rows.</summary>
/// <remarks>Immutable once built; safe to share. Lookups ignore case and a leading dot.</remarks>
public sealed class FileTypeMapper
{
    private const string RegistryResource = "Registry/file-types.json";

    private static readonly Lazy<FileTypeMapper> Embedded = new(() =>
    {
        using var json = typeof(FileTypeMapper).Assembly.GetManifestResourceStream(RegistryResource)
            ?? throw new InvalidOperationException($"The embedded {RegistryResource} is missing.");
        return Load(json);
    });

    private readonly Dictionary<string, FileTypeModel> byExtension = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, FileTypeModel> byMimeType = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Creates a mapper over <paramref name="types"/>; the first row of a content type answers lookups by it.</summary>
    /// <param name="types">The rows, most common first; extensions must be unique.</param>
    /// <exception cref="ArgumentException">Two rows share an extension.</exception>
    public FileTypeMapper(IEnumerable<FileTypeModel> types)
    {
        ArgumentNullException.ThrowIfNull(types);

        Types = types.ToList();
        foreach (var type in Types)
        {
            if (!byExtension.TryAdd(type.Extension, type))
                throw new ArgumentException($"The extension '{type.Extension}' appears twice.", nameof(types));
            byMimeType.TryAdd(type.MimeType, type);
        }
    }

    /// <summary>Gets the mapper over the registry this package ships.</summary>
    public static FileTypeMapper Default => Embedded.Value;

    /// <summary>Gets every row, in registry order.</summary>
    public IReadOnlyList<FileTypeModel> Types { get; }

    /// <summary>Creates a mapper from a registry document in the shipped format.</summary>
    /// <param name="json">The registry document.</param>
    /// <returns>The mapper over its rows.</returns>
    /// <exception cref="FormatException">A row is malformed.</exception>
    public static FileTypeMapper Load(Stream json) => new(FileTypeTableParser.Parse(json));

    /// <summary>Maps an extension to its type.</summary>
    /// <param name="extension">The extension, with or without the dot.</param>
    /// <returns>The type, or <see langword="null"/> when no row has the extension.</returns>
    public FileTypeModel? FromExtension(string? extension) =>
        string.IsNullOrEmpty(extension) ? null : byExtension.GetValueOrDefault(extension.TrimStart('.'));

    /// <summary>Maps a file name to the type its extension names.</summary>
    /// <param name="name">The file name; any path part is ignored.</param>
    /// <returns>The type, or <see langword="null"/> when the name has no known extension.</returns>
    public FileTypeModel? FromName(string? name) => FromExtension(FileNameMapper.ExtensionOf(name));

    /// <summary>Maps a content type to the first type stored as it.</summary>
    /// <param name="mimeType">The content type; parameters such as <c>charset</c> are ignored.</param>
    /// <returns>The type, or <see langword="null"/> when no row has the content type.</returns>
    public FileTypeModel? FromMimeType(string? mimeType)
    {
        if (string.IsNullOrWhiteSpace(mimeType))
            return null;

        var semicolon = mimeType.IndexOf(';');
        return byMimeType.GetValueOrDefault((semicolon < 0 ? mimeType : mimeType[..semicolon]).Trim());
    }

    /// <summary>Detects a type from a file's leading bytes: the longest detecting signature that matches wins, and on
    /// a tie the earlier row.</summary>
    /// <param name="head">The file's leading bytes; <see cref="FileTypeConstants.HeadLength"/> covers every signature.</param>
    /// <returns>The type, or <see langword="null"/> when no signature matches.</returns>
    public FileTypeModel? FromContent(ReadOnlySpan<byte> head)
    {
        FileTypeModel? best = null;
        var bestLength = 0;
        foreach (var type in Types)
        {
            foreach (var signature in type.Signatures)
            {
                if (signature.Detects && signature.Length > bestLength && signature.Match(head) == FileSignatureMatch.Match)
                {
                    best = type;
                    bestLength = signature.Length;
                }
            }
        }

        return best;
    }

    /// <summary>Finds a type, other than <paramref name="claimed"/>, whose distinctive signature the bytes match.</summary>
    /// <param name="head">The file's leading bytes.</param>
    /// <param name="claimed">The type the file claims to be.</param>
    /// <returns>The first such type, or <see langword="null"/>.</returns>
    internal FileTypeModel? FindContradiction(ReadOnlySpan<byte> head, FileTypeModel claimed)
    {
        foreach (var type in Types)
        {
            if (type.Kind == claimed.Kind && type.MimeType == claimed.MimeType)
                continue;
            foreach (var signature in type.Signatures)
            {
                if (signature.IsDistinctive && signature.Match(head) == FileSignatureMatch.Match)
                    return type;
            }
        }

        return null;
    }
}
