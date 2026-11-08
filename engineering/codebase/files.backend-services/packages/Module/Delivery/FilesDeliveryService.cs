using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WoW.Two.Sdk.Beta.Be.Files.Formats.Types;
using WoW.Two.Sdk.Beta.Be.Files.Module.Audit;
using WoW.Two.Sdk.Beta.Be.Files.Module.Blobs;
using WoW.Two.Sdk.Beta.Be.Files.Module.Persistence;
using WoW.Two.Sdk.Beta.Be.Files.Module.Store;
using WoW.Two.Sdk.Beta.Be.Files.Module.Urls;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Files.Urls;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Delivery;

/// <summary>Finds and opens what the content host serves, checking a signed URL with the issuer that signed it. A
/// variant is served while its source file is: live, not deleted, ready, and public for a public URL.</summary>
internal sealed class FilesDeliveryService(
    FilesDbContext db,
    FileContentReader reader,
    FileAuditWriter audit,
    SignedFileUrlIssuer issuer,
    IOptions<FilesModuleOptions> options,
    TimeProvider time) : IFilesDeliveryService
{
    /// <inheritdoc />
    public async Task<Result<DeliverableFileModel>> FindSignedAsync(string? tenantId, FileId id, long expires, string? disposition, string? signature, int? width = null, CancellationToken cancellationToken = default)
    {
        var tenant = tenantId ?? options.Value.DefaultTenantId;
        if (!FileBlobKeyMapper.IsValidTenant(tenant) || disposition is not ("i" or "a") || !issuer.Verify(tenant, id, expires, disposition, signature, width))
            return Result<DeliverableFileModel>.Fail(FilesErrors.UrlInvalid());

        var rowId = FileEntityMapper.ToRowId(id);
        var row = await db.LiveFiles(tenant, time.GetUtcNow()).AsNoTracking()
            .FirstOrDefaultAsync(file => file.Id == rowId && file.DeletedAtUtc == null, cancellationToken);
        if (row is null)
            return Result<DeliverableFileModel>.Fail(FilesErrors.FileNotFound());

        var flag = disposition == "a" ? FileDisposition.Attachment : FileDisposition.Inline;
        var expiresAtUtc = DateTimeOffset.FromUnixTimeSeconds(expires);
        if (width is not { } asked)
            return Result<DeliverableFileModel>.Ok(ToDeliverable(row, flag, expiresAtUtc));

        var variant = await db.Variants.AsNoTracking()
            .FirstOrDefaultAsync(found => found.TenantId == tenant && found.FileId == rowId && found.Width == asked, cancellationToken);
        return variant is null
            ? Result<DeliverableFileModel>.Fail(FilesErrors.FileNotFound())
            : Result<DeliverableFileModel>.Ok(ToDeliverable(row, variant, flag, expiresAtUtc));
    }

    /// <inheritdoc />
    public async Task<Result<DeliverableFileModel>> FindPublicAsync(string? tenantId, string sha256, CancellationToken cancellationToken = default)
    {
        var tenant = tenantId ?? options.Value.DefaultTenantId;
        if (!FileBlobKeyMapper.IsValidTenant(tenant))
            return Result<DeliverableFileModel>.Fail(FilesErrors.FileNotFound());

        var servable = db.LiveFiles(tenant, time.GetUtcNow()).AsNoTracking()
            .Where(file => file.DeletedAtUtc == null && file.Visibility == FileVisibility.Public && file.State == FileState.Ready);
        var row = await servable
            .Where(file => file.Sha256 == sha256)
            .OrderBy(file => file.Seq)
            .FirstOrDefaultAsync(cancellationToken);
        if (row is not null)
            return Result<DeliverableFileModel>.Ok(ToDeliverable(row, FileDisposition.Inline, expiresAtUtc: null));

        var found = await servable
            .Join(db.Variants.AsNoTracking().Where(variant => variant.TenantId == tenant && variant.Sha256 == sha256),
                file => file.Id, variant => variant.FileId, (file, variant) => new { File = file, Variant = variant })
            .OrderBy(pair => pair.File.Seq)
            .FirstOrDefaultAsync(cancellationToken);
        return found is null
            ? Result<DeliverableFileModel>.Fail(FilesErrors.FileNotFound())
            : Result<DeliverableFileModel>.Ok(ToDeliverable(found.File, found.Variant, FileDisposition.Inline, expiresAtUtc: null));
    }

    /// <inheritdoc />
    public async Task<Result<FileContent>> OpenAsync(DeliverableFileModel file, ByteRange? range, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        var rowId = FileEntityMapper.ToRowId(file.Id);
        var row = await db.Files.AsNoTracking().FirstOrDefaultAsync(entity => entity.Id == rowId && entity.TenantId == file.TenantId, cancellationToken);
        if (row is null)
            return Result<FileContent>.Fail(FilesErrors.FileNotFound());

        Result<FileContent> opened;
        if (file.VariantWidth is { } width)
        {
            var variant = await db.Variants.AsNoTracking()
                .FirstOrDefaultAsync(found => found.TenantId == file.TenantId && found.FileId == rowId && found.Width == width, cancellationToken);
            opened = variant is null
                ? Result<FileContent>.Fail(FilesErrors.FileNotFound())
                : row.State != FileState.Ready
                    ? Result<FileContent>.Fail(FilesErrors.FileNotReady())
                    : await reader.OpenAsync(variant, range, cancellationToken);
        }
        else
        {
            opened = await reader.OpenAsync(row, range, cancellationToken);
        }

        if (opened is Result<FileContent>.Success { Value: var content })
            await audit.RecordRedemptionAsync(row.TenantId, row.Id, content.Length, cancellationToken);
        return opened;
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

    /// <summary>Maps a variant to what the content host serves: the variant's bytes, named after the source and width.</summary>
    private static DeliverableFileModel ToDeliverable(FileEntity row, FileVariantEntity variant, FileDisposition disposition, DateTimeOffset? expiresAtUtc)
    {
        var type = FileTypeMapper.Default.FromMimeType(variant.ContentType);
        var stem = Path.GetFileNameWithoutExtension(row.Name);
        return new DeliverableFileModel
        {
            TenantId = row.TenantId,
            Id = new FileId(Guid.Parse(row.Id)),
            Name = FileNameMapper.WithExtension(
                string.Create(CultureInfo.InvariantCulture, $"{(stem.Length > 0 ? stem : "file")}-{variant.Width}"),
                type?.Extension is { Length: > 0 } extension ? extension : "bin"),
            ContentType = variant.ContentType,
            Size = variant.Size,
            Sha256 = variant.Sha256,
            Risk = type?.Risk ?? FileRisk.None,
            Disposition = disposition,
            ExpiresAtUtc = expiresAtUtc,
            VariantWidth = variant.Width,
        };
    }
}
