using WoW.Two.Sdk.Beta.Be.Files.Module.Persistence;
using WoW.Two.Sdk.Beta.Be.Files.Store;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Store;

/// <summary>Maps a stored file row to the contract's <see cref="FileModel"/>.</summary>
internal static class FileEntityMapper
{
    /// <summary>Maps the row to the model.</summary>
    public static FileModel ToModel(FileEntity row) => new()
    {
        Id = new FileId(Guid.Parse(row.Id)),
        TenantId = row.TenantId,
        OrganizationId = row.OrganizationId,
        AppId = row.AppId,
        OwnerId = row.OwnerId,
        Name = row.Name,
        ContentType = row.ContentType,
        Kind = Enum.Parse<FileKind>(row.Kind),
        Size = row.Size,
        Sha256 = row.Sha256,
        Visibility = Enum.Parse<FileVisibility>(row.Visibility),
        State = Enum.Parse<FileState>(row.State),
        CreatedAtUtc = row.CreatedAtUtc,
        ExpiresAtUtc = row.ExpiresAtUtc,
        DeletedAtUtc = row.DeletedAtUtc,
    };

    /// <summary>Maps a file id to the row's id text.</summary>
    public static string ToRowId(FileId id) => id.Value.ToString("D");
}
