using WoW.Two.Sdk.Beta.Be.Files.Audit;
using WoW.Two.Sdk.Beta.Be.Files.Module.Callers;
using WoW.Two.Sdk.Beta.Be.Files.Module.Persistence;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Audit;

/// <summary>Records audit entries for the current caller: added to the pending save, or saved on their own.</summary>
internal sealed class FileAuditWriter(FilesDbContext db, IFilesCallerService callers, TimeProvider time)
{
    /// <summary>Adds an entry for the caller to the pending save.</summary>
    public void Add(string tenantId, string? fileId, FileAuditAction action, long? bytes = null, string? appId = null) =>
        db.Audit.Add(Entry(tenantId, fileId, action, FileAuditOutcome.Ok, errorCode: null, bytes, appId, anonymous: false));

    /// <summary>Saves an entry for the caller on its own.</summary>
    public Task RecordAsync(string tenantId, string? fileId, FileAuditAction action, long? bytes, CancellationToken cancellationToken)
    {
        db.Audit.Add(Entry(tenantId, fileId, action, FileAuditOutcome.Ok, errorCode: null, bytes, appId: null, anonymous: false));
        return db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Saves a refusal for the caller on its own.</summary>
    public Task RecordRefusalAsync(string tenantId, FileAuditAction action, AppError error, string? appId, CancellationToken cancellationToken)
    {
        db.Audit.Add(Entry(tenantId, fileId: null, action, FileAuditOutcome.Refused, error.Type.ToString(), bytes: null, appId, anonymous: false));
        return db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Saves a URL redemption, which no caller makes, on its own.</summary>
    public Task RecordRedemptionAsync(string tenantId, string fileId, long bytes, CancellationToken cancellationToken)
    {
        db.Audit.Add(Entry(tenantId, fileId, FileAuditAction.Download, FileAuditOutcome.Ok, errorCode: null, bytes, appId: null, anonymous: true));
        return db.SaveChangesAsync(cancellationToken);
    }

    private FileAuditEntity Entry(string tenantId, string? fileId, FileAuditAction action, FileAuditOutcome outcome, string? errorCode, long? bytes, string? appId, bool anonymous)
    {
        var caller = anonymous ? null : callers.Current;
        return new FileAuditEntity
        {
            TenantId = tenantId,
            FileId = fileId,
            Action = action.ToString(),
            Outcome = outcome.ToString(),
            ErrorCode = errorCode,
            SubjectId = caller?.SubjectId,
            AppId = appId ?? caller?.AppId,
            Bytes = bytes,
            OccurredAtUtc = time.GetUtcNow(),
        };
    }
}
