using Microsoft.EntityFrameworkCore;
using WoW.Two.Sdk.Beta.Be.Files.Formats.Types;
using WoW.Two.Sdk.Beta.Be.Files.Module.Blobs;
using WoW.Two.Sdk.Beta.Be.Files.Module.Callers;
using WoW.Two.Sdk.Beta.Be.Files.Module.Persistence;
using WoW.Two.Sdk.Beta.Be.Files.Module.Store;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Files.Urls;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Urls;

/// <summary>Integrates the in-process URLs: signed links to the caller's tenant's ready files, and the immutable link
/// of a public one, on the content host.</summary>
internal sealed class ModuleFileUrlsBroker(
    FilesDbContext db,
    SignedFileUrlIssuer issuer,
    IFilesCallerService callers,
    TimeProvider time) : IFileUrlsBroker
{
    /// <inheritdoc />
    public async Task<Result<SignedFileUrl>> IssueAsync(FileId id, UrlSpec spec, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(spec);

        if (spec.Variant is not null)
            return Result<SignedFileUrl>.Fail(FilesErrors.VariantNotFound(spec.Variant));
        if (await FindAsync(id, cancellationToken) is not { } row)
            return Result<SignedFileUrl>.Fail(FilesErrors.FileNotFound());
        if (row.State != nameof(FileState.Ready))
            return Result<SignedFileUrl>.Fail(FilesErrors.FileNotReady());

        return Result<SignedFileUrl>.Ok(issuer.Issue(row.TenantId, id, spec.Lifetime, spec.Disposition));
    }

    /// <inheritdoc />
    public async Task<Result<Uri>> GetPublicUrlAsync(FileId id, string? variant = null, CancellationToken cancellationToken = default)
    {
        if (variant is not null)
            return Result<Uri>.Fail(FilesErrors.VariantNotFound(variant));
        if (await FindAsync(id, cancellationToken) is not { } row)
            return Result<Uri>.Fail(FilesErrors.FileNotFound());
        if (row.Visibility != nameof(FileVisibility.Public))
            return Result<Uri>.Fail(FilesErrors.FileNotPublic());
        if (row.State != nameof(FileState.Ready))
            return Result<Uri>.Fail(FilesErrors.FileNotReady());

        var extension = FileTypeMapper.Default.FromMimeType(row.ContentType)?.Extension is { Length: > 0 } known ? known : "bin";
        return Result<Uri>.Ok(issuer.PublicUrl(row.TenantId, row.Sha256, extension));
    }

    private async Task<FileEntity?> FindAsync(FileId id, CancellationToken cancellationToken)
    {
        var tenantId = callers.Current.TenantId;
        if (!FileBlobKeyMapper.IsValidTenant(tenantId))
            return null;

        var rowId = FileEntityMapper.ToRowId(id);
        return await db.LiveFiles(tenantId, time.GetUtcNow()).AsNoTracking()
            .FirstOrDefaultAsync(file => file.Id == rowId && file.DeletedAtUtc == null, cancellationToken);
    }
}
