using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WoW.Two.Sdk.Beta.Be.Files.Formats.Types;
using WoW.Two.Sdk.Beta.Be.Files.Module.Blobs;
using WoW.Two.Sdk.Beta.Be.Files.Module.Callers;
using WoW.Two.Sdk.Beta.Be.Files.Module.Persistence;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Store;

/// <summary>Integrates the in-process store: the caller's tenant's file rows and their bytes in the blob store.</summary>
/// <remarks>A file past its purge or orphan time, or expired with no reference left, is gone for every call, even
/// before the sweep purges it.</remarks>
internal sealed class ModuleFileStoreBroker(
    FilesDbContext db,
    FileContentReader reader,
    IFilesCallerService callers,
    IOptions<FilesModuleOptions> options,
    TimeProvider time) : IFileStoreBroker
{
    /// <inheritdoc />
    public async Task<Result<FileModel>> GetAsync(FileId id, CancellationToken cancellationToken = default) =>
        await FindAsync(id, includeDeleted: true, cancellationToken) is { } row
            ? Result<FileModel>.Ok(FileEntityMapper.ToModel(row))
            : Result<FileModel>.Fail(FilesErrors.FileNotFound());

    /// <inheritdoc />
    public async Task<Result<FileContent>> OpenReadAsync(FileId id, ByteRange? range = null, CancellationToken cancellationToken = default) =>
        await FindAsync(id, includeDeleted: false, cancellationToken) is { } row
            ? await reader.OpenAsync(row, range, cancellationToken)
            : Result<FileContent>.Fail(FilesErrors.FileNotFound());

    /// <inheritdoc />
    public async Task<Result<FilePage>> ListAsync(FileQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        long? before = null;
        if (query.Cursor is not null)
        {
            if (!long.TryParse(query.Cursor, NumberStyles.None, CultureInfo.InvariantCulture, out var seq))
                return Result<FilePage>.Fail(FilesErrors.CursorInvalid());
            before = seq;
        }

        if (Live() is not { } rows)
            return Result<FilePage>.Fail(FilesErrors.TenantInvalid());
        if (!query.IncludeDeleted)
            rows = rows.Where(file => file.DeletedAtUtc == null);
        if (query.AppId is not null)
            rows = rows.Where(file => file.AppId == query.AppId);
        if (query.OwnerId is not null)
            rows = rows.Where(file => file.OwnerId == query.OwnerId);
        if (query.Kind is { } kind)
            rows = rows.Where(file => file.Kind == kind.ToString());
        if (query.State is { } state)
            rows = rows.Where(file => file.State == state.ToString());
        if (query.CreatedAfterUtc is { } createdAfter)
        {
            var after = createdAfter.ToUniversalTime();
            rows = rows.Where(file => file.CreatedAtUtc > after);
        }
        if (before is { } cursor)
            rows = rows.Where(file => file.Seq < cursor);

        var limit = Math.Clamp(query.Limit, 1, FilesConstants.MaxPageSize);
        var page = await rows.AsNoTracking().OrderByDescending(file => file.Seq).Take(limit + 1).ToListAsync(cancellationToken);
        var next = page.Count > limit ? page[limit - 1].Seq.ToString(CultureInfo.InvariantCulture) : null;

        return Result<FilePage>.Ok(new FilePage(page.Take(limit).Select(FileEntityMapper.ToModel).ToList(), next));
    }

    /// <inheritdoc />
    public async Task<Result<FileModel>> UpdateAsync(FileId id, FilePatch patch, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(patch);

        if (await FindAsync(id, includeDeleted: false, cancellationToken) is not { } row)
            return Result<FileModel>.Fail(FilesErrors.FileNotFound());

        if (patch.Name is not null)
        {
            if (FileNameMapper.ToSafeName(patch.Name) is not Result<string>.Success { Value: var name })
                return Result<FileModel>.Fail(FilesErrors.NameInvalid());

            // A new extension must name the stored type, so a rename never turns checked bytes into another type.
            var claimed = FileTypeMapper.Default.FromName(name);
            if (claimed is not null && claimed.Kind != FileKind.Generic && claimed.MimeType != row.ContentType)
                return Result<FileModel>.Fail(FilesErrors.ContentMismatch());
            row.Name = name;
        }

        if (patch.Visibility is { } visibility)
            row.Visibility = visibility.ToString();
        if (patch.ExpiresAtUtc is { } expiresAt)
            row.ExpiresAtUtc = expiresAt.ToUniversalTime();

        await db.SaveChangesAsync(cancellationToken);
        return Result<FileModel>.Ok(FileEntityMapper.ToModel(row));
    }

    /// <inheritdoc />
    public async Task<Result<FileModel>> DeleteAsync(FileId id, CancellationToken cancellationToken = default)
    {
        if (await FindAsync(id, includeDeleted: true, cancellationToken) is not { } row)
            return Result<FileModel>.Fail(FilesErrors.FileNotFound());

        if (row.DeletedAtUtc is null)
        {
            var now = time.GetUtcNow();
            row.DeletedAtUtc = now;
            row.PurgeAtUtc = now + options.Value.PolicyFor(row.AppId).DeletedRetention;
            await db.SaveChangesAsync(cancellationToken);
        }

        return Result<FileModel>.Ok(FileEntityMapper.ToModel(row));
    }

    /// <inheritdoc />
    public async Task<Result<FileModel>> RestoreAsync(FileId id, CancellationToken cancellationToken = default)
    {
        if (await FindAsync(id, includeDeleted: true, cancellationToken) is not { } row)
            return Result<FileModel>.Fail(FilesErrors.FileNotFound());

        if (row.DeletedAtUtc is not null)
        {
            row.DeletedAtUtc = null;
            row.PurgeAtUtc = null;
            await db.SaveChangesAsync(cancellationToken);
        }

        return Result<FileModel>.Ok(FileEntityMapper.ToModel(row));
    }

    /// <summary>Finds a live file of the caller's tenant, tracked for change.</summary>
    private async Task<FileEntity?> FindAsync(FileId id, bool includeDeleted, CancellationToken cancellationToken)
    {
        if (Live() is not { } rows)
            return null;

        var rowId = FileEntityMapper.ToRowId(id);
        var row = await rows.FirstOrDefaultAsync(file => file.Id == rowId, cancellationToken);
        return row is not null && (includeDeleted || row.DeletedAtUtc is null) ? row : null;
    }

    /// <summary>Gets the caller's tenant's live files; none for a tenant id that is not a safe key segment.</summary>
    private IQueryable<FileEntity>? Live()
    {
        var tenantId = callers.Current.TenantId;
        return FileBlobKeyMapper.IsValidTenant(tenantId) ? db.LiveFiles(tenantId, time.GetUtcNow()) : null;
    }
}
