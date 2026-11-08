using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WoW.Two.Sdk.Beta.Be.Files.Audit;
using WoW.Two.Sdk.Beta.Be.Files.Formats.Types;
using WoW.Two.Sdk.Beta.Be.Files.Module.Audit;
using WoW.Two.Sdk.Beta.Be.Files.Module.Blobs;
using WoW.Two.Sdk.Beta.Be.Files.Module.Callers;
using WoW.Two.Sdk.Beta.Be.Files.Module.Persistence;
using WoW.Two.Sdk.Beta.Be.Files.Module.Policies;
using WoW.Two.Sdk.Beta.Be.Files.Module.Store;
using WoW.Two.Sdk.Beta.Be.Files.Module.Variants;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Files.Uploads;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Uploads;

/// <summary>Integrates the in-process upload: spools and hashes the bytes, checks them against the name and the app
/// policy, then stores them once per tenant, records the file, and makes the image variants the policy names.</summary>
internal sealed class ModuleFileUploadsBroker(
    FilesDbContext db,
    FileBlobWriter writer,
    FileVariantService variants,
    FileVariantReader variantReader,
    IFilesCallerService callers,
    IOptions<FilesModuleOptions> options,
    FileBlobLocks locks,
    FileAuditWriter audit,
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
            return Result<FileModel>.Ok(await ToModelAsync(earlier, cancellationToken));

        if (caller.AppId is not null && spec.AppId is not null && spec.AppId != caller.AppId)
            return await RefuseAsync(caller.TenantId, FilesErrors.AppNotAllowed(), spec.AppId, cancellationToken);

        var appId = spec.AppId ?? caller.AppId;
        var policy = options.Value.PolicyFor(appId);
        var spooled = await UploadSpool.CreateAsync(content, policy.MaxBytes, options.Value.TempPath ?? Path.GetTempPath(), cancellationToken);
        if (spooled is Result<UploadSpool>.Failure tooLarge)
            return await RefuseAsync(caller.TenantId, tooLarge.Error, appId, cancellationToken);

        await using var spool = ((Result<UploadSpool>.Success)spooled).Value;
        var recognized = FileContentValidator.Default.Validate(spec.Name, spec.ContentType, spool.Head, spool.HeadIsComplete);
        if (recognized is Result<RecognizedFileModel>.Failure invalid)
            return await RefuseAsync(caller.TenantId, invalid.Error, appId, cancellationToken);

        var file = ((Result<RecognizedFileModel>.Success)recognized).Value;
        if (!Allows(policy, file.Type))
            return await RefuseAsync(caller.TenantId, FilesErrors.TypeNotAllowed(file.Type.MimeType), appId, cancellationToken);

        var now = time.GetUtcNow();
        var row = new FileEntity
        {
            Id = FileEntityMapper.ToRowId(FileId.New()),
            TenantId = caller.TenantId,
            OrganizationId = caller.OrganizationId,
            AppId = appId,
            OwnerId = caller.SubjectId,
            OwnerIssuer = caller.Issuer,
            Name = file.Name,
            ContentType = file.Type.MimeType,
            Kind = file.Type.Kind,
            Size = spool.Size,
            Sha256 = spool.Sha256,
            Visibility = spec.Visibility,
            State = FileState.Ready,
            IdempotencyKey = idempotencyKey,
            CreatedAtUtc = now,
            ExpiresAtUtc = spec.ExpiresAtUtc?.ToUniversalTime() ?? now + policy.DefaultLifetime,
            OrphanPurgeAtUtc = now + policy.OrphanRetention,
        };

        using (await locks.AcquireAsync(spool.Sha256, cancellationToken))
        {
            await writer.StoreAsync(caller.TenantId, spool.Sha256, spool.Size, spool.OpenRead, file.Type.MimeType, now, cancellationToken);
            db.Files.Add(row);
            audit.Add(caller.TenantId, row.Id, FileAuditAction.Upload, spool.Size, appId);
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
                return Result<FileModel>.Ok(await ToModelAsync(raced, cancellationToken));
            }
        }

        // The file is stored: its variants finish even if the caller goes away, and a failing one is skipped.
        var made = await variants.CreateAsync(row, file.Type, spool.OpenRead, policy, now, CancellationToken.None);
        return Result<FileModel>.Ok(FileEntityMapper.ToModel(row, made));
    }

    /// <summary>Maps an earlier upload's file with its variants.</summary>
    private async Task<FileModel> ToModelAsync(FileEntity row, CancellationToken cancellationToken) =>
        FileEntityMapper.ToModel(row, await variantReader.ListAsync(row.TenantId, row.Id, cancellationToken));

    /// <summary>Records a refused upload and answers with its error.</summary>
    private async Task<Result<FileModel>> RefuseAsync(string tenantId, Foundation.Errors.AppError error, string? appId, CancellationToken cancellationToken)
    {
        await audit.RecordRefusalAsync(tenantId, FileAuditAction.Upload, error, appId, cancellationToken);
        return Result<FileModel>.Fail(error);
    }

    private Task<FileEntity?> FindByKeyAsync(string tenantId, string idempotencyKey, CancellationToken cancellationToken) =>
        db.Files.AsNoTracking().FirstOrDefaultAsync(file => file.TenantId == tenantId && file.IdempotencyKey == idempotencyKey, cancellationToken);

    /// <summary>Tells whether the app's policy accepts the type.</summary>
    private static bool Allows(FilePolicyOptions policy, FileTypeModel type) =>
        (policy.AllowExecutables || type.Risk != FileRisk.Executable)
        && (policy.AllowedKinds.Count == 0 || policy.AllowedKinds.Contains(type.Kind))
        && (policy.AllowedExtensions.Count == 0 || policy.AllowedExtensions.Contains(type.Extension));
}
