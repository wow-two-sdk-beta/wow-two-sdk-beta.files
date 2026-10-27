using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WoW.Two.Sdk.Beta.Be.Files.Formats.Types;
using WoW.Two.Sdk.Beta.Be.Files.Module.Blobs;
using WoW.Two.Sdk.Beta.Be.Files.Module.Callers;
using WoW.Two.Sdk.Beta.Be.Files.Module.Persistence;
using WoW.Two.Sdk.Beta.Be.Files.Module.Policies;
using WoW.Two.Sdk.Beta.Be.Files.Module.Store;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Files.Uploads;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;
using WoW.Two.Sdk.Beta.Be.Storage.Core;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Uploads;

/// <summary>Integrates the in-process upload: spools and hashes the bytes, checks them against the name and the app
/// policy, then stores them once per tenant and records the file.</summary>
internal sealed class ModuleFileUploadsBroker(
    FilesDbContext db,
    IBlobRepository blobs,
    IFilesCallerService callers,
    IOptions<FilesModuleOptions> options,
    FileBlobLocks locks,
    TimeProvider time) : IFileUploadsBroker
{
    /// <inheritdoc />
    public async Task<Result<FileModel>> UploadAsync(Stream content, UploadSpec spec, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(spec);

        var caller = callers.Current;
        if (!FileBlobKeyMapper.IsValidTenant(caller.TenantId))
            return Result<FileModel>.Fail(FilesErrors.TenantInvalid());

        var idempotencyKey = string.IsNullOrWhiteSpace(spec.IdempotencyKey) ? null : spec.IdempotencyKey;
        if (idempotencyKey is not null && await FindByKeyAsync(caller.TenantId, idempotencyKey, cancellationToken) is { } earlier)
            return Result<FileModel>.Ok(FileEntityMapper.ToModel(earlier));

        var appId = spec.AppId ?? caller.AppId;
        var policy = options.Value.PolicyFor(appId);
        var spooled = await UploadSpool.CreateAsync(content, policy.MaxBytes, options.Value.TempPath ?? Path.GetTempPath(), cancellationToken);
        if (spooled is Result<UploadSpool>.Failure tooLarge)
            return Result<FileModel>.Fail(tooLarge.Error);

        await using var spool = ((Result<UploadSpool>.Success)spooled).Value;
        var recognized = FileContentValidator.Default.Validate(spec.Name, spec.ContentType, spool.Head, spool.HeadIsComplete);
        if (recognized is Result<RecognizedFileModel>.Failure invalid)
            return Result<FileModel>.Fail(invalid.Error);

        var file = ((Result<RecognizedFileModel>.Success)recognized).Value;
        if (!Allows(policy, file.Type))
            return Result<FileModel>.Fail(FilesErrors.TypeNotAllowed(file.Type.MimeType));

        var now = time.GetUtcNow();
        var row = new FileEntity
        {
            Id = FileEntityMapper.ToRowId(FileId.New()),
            TenantId = caller.TenantId,
            OrganizationId = caller.OrganizationId,
            AppId = appId,
            OwnerId = caller.SubjectId,
            Name = file.Name,
            ContentType = file.Type.MimeType,
            Kind = file.Type.Kind.ToString(),
            Size = spool.Size,
            Sha256 = spool.Sha256,
            Visibility = spec.Visibility.ToString(),
            State = nameof(FileState.Ready),
            IdempotencyKey = idempotencyKey,
            CreatedAtUtc = now,
            ExpiresAtUtc = spec.ExpiresAtUtc?.ToUniversalTime(),
        };

        using (await locks.AcquireAsync(spool.Sha256, cancellationToken))
        {
            await StoreBytesAsync(caller.TenantId, spool, file.Type.MimeType, now, cancellationToken);
            db.Files.Add(row);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException) when (idempotencyKey is not null)
            {
                // A concurrent retry with the same key won the insert; answer with its file.
                db.ChangeTracker.Clear();
                if (await FindByKeyAsync(caller.TenantId, idempotencyKey, cancellationToken) is not { } raced)
                    throw;
                return Result<FileModel>.Ok(FileEntityMapper.ToModel(raced));
            }
        }

        return Result<FileModel>.Ok(FileEntityMapper.ToModel(row));
    }

    /// <summary>Reuses the tenant's copy of the bytes, or stores them and adds its row to the pending save.</summary>
    private async Task StoreBytesAsync(string tenantId, UploadSpool spool, string contentType, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var reused = await db.Blobs
            .Where(blob => blob.TenantId == tenantId && blob.Sha256 == spool.Sha256)
            .ExecuteUpdateAsync(set => set.SetProperty(blob => blob.LastUsedAtUtc, now), cancellationToken);
        if (reused > 0)
            return;

        var key = FileBlobKeyMapper.ToKey(tenantId, spool.Sha256);
        await using (var bytes = spool.OpenRead())
            await blobs.SaveAsync(key, bytes, contentType, cancellationToken);

        db.Blobs.Add(new FileBlobEntity
        {
            TenantId = tenantId,
            Sha256 = spool.Sha256,
            StorageKey = key,
            Size = spool.Size,
            CreatedAtUtc = now,
            LastUsedAtUtc = now,
        });
    }

    private Task<FileEntity?> FindByKeyAsync(string tenantId, string idempotencyKey, CancellationToken cancellationToken) =>
        db.Files.AsNoTracking().FirstOrDefaultAsync(file => file.TenantId == tenantId && file.IdempotencyKey == idempotencyKey, cancellationToken);

    /// <summary>Tells whether the app's policy accepts the type.</summary>
    private static bool Allows(FilePolicyOptions policy, FileTypeModel type) =>
        (policy.AllowExecutables || type.Risk != FileRisk.Executable)
        && (policy.AllowedKinds.Count == 0 || policy.AllowedKinds.Contains(type.Kind))
        && (policy.AllowedExtensions.Count == 0 || policy.AllowedExtensions.Contains(type.Extension));
}
