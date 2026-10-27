using WoW.Two.Sdk.Beta.Be.Files.Store;

namespace WoW.Two.Sdk.Beta.Be.Files.Formats.Types;

/// <summary>Represents one row of the file-type registry: what a file with this extension is.</summary>
public sealed record FileTypeModel
{
    /// <summary>Gets the extension, lowercase and without the dot; empty for bytes no row describes.</summary>
    public required string Extension { get; init; }

    /// <summary>Gets the content type the file is stored and served as.</summary>
    public required string MimeType { get; init; }

    /// <summary>Gets the kind, which picks the icon, the colour and the policy group.</summary>
    public required FileKind Kind { get; init; }

    /// <summary>Gets the short text the icon shows.</summary>
    public required string Label { get; init; }

    /// <summary>Gets the alternative magic-byte signatures; a type with none is recognized by its name only.</summary>
    public IReadOnlyList<FileSignatureModel> Signatures { get; init; } = [];

    /// <summary>Gets the previewer that shows the file.</summary>
    public FilePreview Preview { get; init; }

    /// <summary>Gets what the file can do when opened.</summary>
    public FileRisk Risk { get; init; }

    /// <summary>Creates the type of a file no row describes: generic, served as plain bytes.</summary>
    /// <param name="extension">The file's extension, when it has one; it becomes the icon's label.</param>
    /// <returns>The generic type.</returns>
    public static FileTypeModel Unknown(string extension = "") => new()
    {
        Extension = extension,
        MimeType = FileTypeConstants.OctetStream,
        Kind = FileKind.Generic,
        Label = extension.ToUpperInvariant(),
    };
}
