using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;

namespace WoW.Two.Sdk.Beta.Be.Files.Formats.Types;

/// <summary>Validates a file's name and leading bytes against the type its name claims, and recognizes its type.</summary>
/// <remarks>
/// The claimed type comes from the name's extension, or from the declared content type when the name has none. A
/// claimed type with signatures must match one; a claimed type without any fails when the bytes carry another type's
/// distinctive signature. A generic or unknown extension passes, and its type is detected from the bytes. The stored
/// content type is always the registry's, never the uploader's.
/// </remarks>
/// <param name="types">The registry to check against.</param>
public sealed class FileContentValidator(FileTypeMapper types)
{
    /// <summary>Gets the validator over the registry this package ships.</summary>
    public static FileContentValidator Default { get; } = new(FileTypeMapper.Default);

    /// <summary>Validates a file and recognizes its type.</summary>
    /// <param name="name">The name the uploader gave.</param>
    /// <param name="declaredContentType">The content type the uploader declared; it only counts when the name has no
    /// extension.</param>
    /// <param name="head">The file's leading bytes, up to <see cref="FileTypeConstants.HeadLength"/>.</param>
    /// <param name="complete">Whether <paramref name="head"/> is the whole file, so a signature it cannot reach fails
    /// instead of passing.</param>
    /// <returns>The safe name and the type; <see cref="FilesErrors.NameInvalid"/> or
    /// <see cref="FilesErrors.ContentMismatch"/> otherwise.</returns>
    public Result<RecognizedFileModel> Validate(string? name, string? declaredContentType, ReadOnlySpan<byte> head, bool complete = false)
    {
        var sanitized = FileNameMapper.ToSafeName(name);
        if (sanitized is Result<string>.Failure invalid)
            return Result<RecognizedFileModel>.Fail(invalid.Error);

        var safeName = ((Result<string>.Success)sanitized).Value;
        var extension = FileNameMapper.ExtensionOf(safeName);
        var claimed = extension.Length > 0 ? types.FromExtension(extension) : types.FromMimeType(declaredContentType);

        if (claimed is null)
        {
            var detected = types.FromContent(head);
            return Recognized(safeName, extension, detected ?? FileTypeModel.Unknown(extension));
        }

        if (claimed.Kind == FileKind.Generic)
            return Recognized(safeName, extension, claimed);

        var contradicted = claimed.Signatures.Count > 0
            ? !MatchesAny(claimed.Signatures, head, complete)
            : types.FindContradiction(head, claimed) is not null;

        return contradicted
            ? Result<RecognizedFileModel>.Fail(FilesErrors.ContentMismatch())
            : Recognized(safeName, extension, claimed);
    }

    /// <summary>Tells whether the bytes match one of the claimed type's signatures, or end before one can be told.</summary>
    private static bool MatchesAny(IReadOnlyList<FileSignatureModel> signatures, ReadOnlySpan<byte> head, bool complete)
    {
        foreach (var signature in signatures)
        {
            var match = signature.Match(head);
            if (match == FileSignatureMatch.Match || (match == FileSignatureMatch.Unknown && !complete))
                return true;
        }

        return false;
    }

    /// <summary>Accepts the file, adding the type's extension to a name that had none.</summary>
    private static Result<RecognizedFileModel> Recognized(string safeName, string extension, FileTypeModel type) =>
        Result<RecognizedFileModel>.Ok(new RecognizedFileModel(
            extension.Length == 0 && type.Extension.Length > 0 ? FileNameMapper.WithExtension(safeName, type.Extension) : safeName,
            type));
}
