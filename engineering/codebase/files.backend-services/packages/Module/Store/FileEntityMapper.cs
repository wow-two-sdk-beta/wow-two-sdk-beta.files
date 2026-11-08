using WoW.Two.Sdk.Beta.Be.Files.Module.Persistence;
using WoW.Two.Sdk.Beta.Be.Files.Store;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Store;

/// <summary>Maps a stored file row, with its variant rows, to the contract's <see cref="FileModel"/>.</summary>
internal static class FileEntityMapper
{
    /// <summary>Maps the row and its variants to the model.</summary>
    /// <param name="row">The file.</param>
    /// <param name="variants">The file's variants in any order; none when it has no variants.</param>
    public static FileModel ToModel(FileEntity row, IReadOnlyCollection<FileVariantEntity>? variants = null) => new()
    {
        Id = new FileId(Guid.Parse(row.Id)),
        TenantId = row.TenantId,
        OrganizationId = row.OrganizationId,
        AppId = row.AppId,
        OwnerId = row.OwnerId,
        Name = row.Name,
        ContentType = row.ContentType,
        Kind = row.Kind,
        Size = row.Size,
        Sha256 = row.Sha256,
        Visibility = row.Visibility,
        State = row.State,
        CreatedAtUtc = row.CreatedAtUtc,
        ExpiresAtUtc = row.ExpiresAtUtc,
        DeletedAtUtc = row.DeletedAtUtc,
        Variants = variants is { Count: > 0 }
            ? variants.OrderBy(variant => variant.Width).Select(ToModel).ToArray()
            : [],
    };

    /// <summary>Maps a variant row to the model.</summary>
    public static FileVariantModel ToModel(FileVariantEntity row) => new()
    {
        Width = row.Width,
        Height = row.Height,
        ContentType = row.ContentType,
        Size = row.Size,
    };

    /// <summary>Maps a file id to the row's id text.</summary>
    public static string ToRowId(FileId id) => id.Value.ToString("D");
}
