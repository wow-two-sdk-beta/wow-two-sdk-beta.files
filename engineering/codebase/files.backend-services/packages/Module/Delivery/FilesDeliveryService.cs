using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WoW.Two.Sdk.Beta.Be.Files.Formats.Types;
using WoW.Two.Sdk.Beta.Be.Files.Module.Blobs;
using WoW.Two.Sdk.Beta.Be.Files.Module.Persistence;
using WoW.Two.Sdk.Beta.Be.Files.Module.Store;
using WoW.Two.Sdk.Beta.Be.Files.Module.Urls;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Files.Urls;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Delivery;

/// <summary>Finds and opens what the content host serves, checking a signed URL with the issuer that signed it.</summary>
internal sealed class FilesDeliveryService(
    FilesDbContext db,
    FileContentReader reader,
    SignedFileUrlIssuer issuer,
    IOptions<FilesModuleOptions> options,
    TimeProvider time) : IFilesDeliveryService
{
    /// <inheritdoc />
    public async Task<Result<DeliverableFileModel>> FindSignedAsync(string? tenantId, FileId id, long expires, string? disposition, string? signature, CancellationToken cancellationToken = default)
    {
        var tenant = tenantId ?? options.Value.DefaultTenantId;
        if (!FileBlobKeyMapper.IsValidTenant(tenant) || disposition is not ("i" or "a") || !issuer.Verify(tenant, id, expires, disposition, signature))
            return Result<DeliverableFileModel>.Fail(FilesErrors.UrlInvalid());

        var rowId = FileEntityMapper.ToRowId(id);
        var row = await db.LiveFiles(tenant, time.GetUtcNow()).AsNoTracking()
            .FirstOrDefaultAsync(file => file.Id == rowId && file.DeletedAtUtc == null, cancellationToken);
        return row is null
            ? Result<DeliverableFileModel>.Fail(FilesErrors.FileNotFound())
            : Result<DeliverableFileModel>.Ok(ToDeliverable(row, disposition == "a" ? FileDisposition.Attachment : FileDisposition.Inline, DateTimeOffset.FromUnixTimeSeconds(expires)));
    }

    /// <inheritdoc />
    public async Task<Result<DeliverableFileModel>> FindPublicAsync(string? tenantId, string sha256, CancellationToken cancellationToken = default)
    {
        var tenant = tenantId ?? options.Value.DefaultTenantId;
        if (!FileBlobKeyMapper.IsValidTenant(tenant))
            return Result<DeliverableFileModel>.Fail(FilesErrors.FileNotFound());

        var row = await db.LiveFiles(tenant, time.GetUtcNow()).AsNoTracking()
            .Where(file => file.Sha256 == sha256 && file.DeletedAtUtc == null
                && file.Visibility == nameof(FileVisibility.Public) && file.State == nameof(FileState.Ready))
            .OrderBy(file => file.Seq)
            .FirstOrDefaultAsync(cancellationToken);
        return row is null
            ? Result<DeliverableFileModel>.Fail(FilesErrors.FileNotFound())
            : Result<DeliverableFileModel>.Ok(ToDeliverable(row, FileDisposition.Inline, expiresAtUtc: null));
    }

    /// <inheritdoc />
    public async Task<Result<FileContent>> OpenAsync(DeliverableFileModel file, ByteRange? range, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        var rowId = FileEntityMapper.ToRowId(file.Id);
        var row = await db.Files.AsNoTracking().FirstOrDefaultAsync(entity => entity.Id == rowId && entity.TenantId == file.TenantId, cancellationToken);
        return row is null
            ? Result<FileContent>.Fail(FilesErrors.FileNotFound())
            : await reader.OpenAsync(row, range, cancellationToken);
    }

    private static DeliverableFileModel ToDeliverable(FileEntity row, FileDisposition disposition, DateTimeOffset? expiresAtUtc) => new()
    {
        TenantId = row.TenantId,
        Id = new FileId(Guid.Parse(row.Id)),
        Name = row.Name,
        ContentType = row.ContentType,
        Size = row.Size,
        Sha256 = row.Sha256,
        Risk = FileTypeMapper.Default.FromMimeType(row.ContentType)?.Risk ?? FileRisk.None,
        Disposition = disposition,
        ExpiresAtUtc = expiresAtUtc,
    };
}
