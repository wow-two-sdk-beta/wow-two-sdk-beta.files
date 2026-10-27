using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WoW.Two.Sdk.Beta.Be.Files.Module.Blobs;
using WoW.Two.Sdk.Beta.Be.Files.Module.Callers;
using WoW.Two.Sdk.Beta.Be.Files.Module.Persistence;
using WoW.Two.Sdk.Beta.Be.Files.Module.Store;
using WoW.Two.Sdk.Beta.Be.Files.References;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.References;

/// <summary>Integrates the in-process references: which of the caller's tenant's records use a file. Attaching keeps
/// the file alive; the last detach starts its orphan retention when the app has one.</summary>
internal sealed class ModuleFileReferencesBroker(
    FilesDbContext db,
    IFilesCallerService callers,
    IOptions<FilesModuleOptions> options,
    TimeProvider time) : IFileReferencesBroker
{
    /// <inheritdoc />
    public async Task<Result> AttachAsync(FileId id, FileReference reference, CancellationToken cancellationToken = default)
    {
        if (!IsValid(reference))
            return Result.Fail(FilesErrors.ReferenceInvalid());
        if (await FindAsync(id, cancellationToken) is not { DeletedAtUtc: null } file)
            return Result.Fail(FilesErrors.FileNotFound());

        var exists = await Rows(file, reference).AnyAsync(cancellationToken);
        if (!exists)
        {
            db.References.Add(new FileReferenceEntity
            {
                TenantId = file.TenantId,
                FileId = file.Id,
                RecordType = reference.RecordType,
                RecordId = reference.RecordId,
                CreatedAtUtc = time.GetUtcNow(),
            });
        }

        file.OrphanPurgeAtUtc = null;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException) when (!exists)
        {
            // A concurrent attach of the same reference won; attaching twice is a no-op.
        }

        return Result.Ok();
    }

    /// <inheritdoc />
    public async Task<Result> DetachAsync(FileId id, FileReference reference, CancellationToken cancellationToken = default)
    {
        if (!IsValid(reference))
            return Result.Fail(FilesErrors.ReferenceInvalid());
        if (await FindAsync(id, cancellationToken) is not { } file)
            return Result.Fail(FilesErrors.FileNotFound());
        if (await Rows(file, reference).ExecuteDeleteAsync(cancellationToken) == 0)
            return Result.Fail(FilesErrors.ReferenceNotFound());

        if (!await db.References.AnyAsync(row => row.FileId == file.Id, cancellationToken))
        {
            file.OrphanPurgeAtUtc = time.GetUtcNow() + options.Value.PolicyFor(file.AppId).OrphanRetention;
            await db.SaveChangesAsync(cancellationToken);
        }

        return Result.Ok();
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<FileReference>>> ListAsync(FileId id, CancellationToken cancellationToken = default)
    {
        if (await FindAsync(id, cancellationToken) is not { } file)
            return Result<IReadOnlyList<FileReference>>.Fail(FilesErrors.FileNotFound());

        var references = await db.References.AsNoTracking()
            .Where(row => row.FileId == file.Id)
            .OrderBy(row => row.CreatedAtUtc)
            .Select(row => new FileReference(row.RecordType, row.RecordId))
            .ToListAsync(cancellationToken);
        return Result<IReadOnlyList<FileReference>>.Ok(references);
    }

    /// <summary>Finds a live file of the caller's tenant, deleted ones included, tracked for change.</summary>
    private async Task<FileEntity?> FindAsync(FileId id, CancellationToken cancellationToken)
    {
        var tenantId = callers.Current.TenantId;
        if (!FileBlobKeyMapper.IsValidTenant(tenantId))
            return null;

        var rowId = FileEntityMapper.ToRowId(id);
        return await db.LiveFiles(tenantId, time.GetUtcNow()).FirstOrDefaultAsync(file => file.Id == rowId, cancellationToken);
    }

    private IQueryable<FileReferenceEntity> Rows(FileEntity file, FileReference reference) =>
        db.References.Where(row => row.FileId == file.Id && row.RecordType == reference.RecordType && row.RecordId == reference.RecordId);

    private static bool IsValid(FileReference reference) =>
        !string.IsNullOrWhiteSpace(reference.RecordType) && !string.IsNullOrWhiteSpace(reference.RecordId)
        && reference.RecordType.Length <= FilesConstants.MaxReferenceLength && reference.RecordId.Length <= FilesConstants.MaxReferenceLength;
}
