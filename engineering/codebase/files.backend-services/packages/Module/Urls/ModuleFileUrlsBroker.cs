using Microsoft.EntityFrameworkCore;
using WoW.Two.Sdk.Beta.Be.Files.Formats.Types;
using WoW.Two.Sdk.Beta.Be.Files.Module.Blobs;
using WoW.Two.Sdk.Beta.Be.Files.Module.Callers;
using WoW.Two.Sdk.Beta.Be.Files.Module.Persistence;
using WoW.Two.Sdk.Beta.Be.Files.Module.Store;
using WoW.Two.Sdk.Beta.Be.Files.Module.Variants;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Files.Urls;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Urls;

/// <summary>Integrates the in-process URLs: signed links to the caller's tenant's ready files, and the immutable link
/// of a public one, on the content host — to the original, or to the variant a width picks.</summary>
internal sealed class ModuleFileUrlsBroker(
    FilesDbContext db,
    SignedFileUrlIssuer issuer,
    FileVariantReader variants,
    IFilesCallerService callers,
    TimeProvider time) : IFileUrlsBroker
{
    /// <inheritdoc />
    public async Task<Result<SignedFileUrl>> IssueAsync(FileId id, UrlSpec spec, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(spec);

        if (spec.Width is <= 0)
            return Result<SignedFileUrl>.Fail(WidthInvalid());
        if (await FindAsync(id, cancellationToken) is not { } row)
            return Result<SignedFileUrl>.Fail(FilesErrors.FileNotFound());
        if (row.State != nameof(FileState.Ready))
            return Result<SignedFileUrl>.Fail(FilesErrors.FileNotReady());

        var variant = spec.Width is { } width ? await SelectAsync(row, width, cancellationToken) : null;
        return Result<SignedFileUrl>.Ok(issuer.Issue(row.TenantId, id, spec.Lifetime, spec.Disposition, variant?.Width));
    }

    /// <inheritdoc />
    public async Task<Result<Uri>> GetPublicUrlAsync(FileId id, int? width = null, CancellationToken cancellationToken = default)
    {
        if (width is <= 0)
            return Result<Uri>.Fail(WidthInvalid());
        if (await FindAsync(id, cancellationToken) is not { } row)
            return Result<Uri>.Fail(FilesErrors.FileNotFound());
        if (row.Visibility != nameof(FileVisibility.Public))
            return Result<Uri>.Fail(FilesErrors.FileNotPublic());
        if (row.State != nameof(FileState.Ready))
            return Result<Uri>.Fail(FilesErrors.FileNotReady());

        if (width is { } asked && await SelectAsync(row, asked, cancellationToken) is { } variant)
            return Result<Uri>.Ok(issuer.PublicUrl(row.TenantId, variant.Sha256, ExtensionOf(variant.ContentType)));
        return Result<Uri>.Ok(issuer.PublicUrl(row.TenantId, row.Sha256, ExtensionOf(row.ContentType)));
    }

    /// <summary>Selects the variant a width picks; none when the file has no variants.</summary>
    private async Task<FileVariantEntity?> SelectAsync(FileEntity row, int width, CancellationToken cancellationToken)
    {
        var found = await variants.ListAsync(row.TenantId, row.Id, cancellationToken);
        return FileVariantWidthMapper.Select(found.Select(variant => variant.Width), width) is { } chosen
            ? found.First(variant => variant.Width == chosen)
            : null;
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

    /// <summary>Maps a content type to the extension its registry row names; <c>bin</c> when none does.</summary>
    private static string ExtensionOf(string contentType) =>
        FileTypeMapper.Default.FromMimeType(contentType)?.Extension is { Length: > 0 } known ? known : "bin";

    private static AppError WidthInvalid() => AppErrorFactory.Validation("The width must be at least 1 pixel.");
}
