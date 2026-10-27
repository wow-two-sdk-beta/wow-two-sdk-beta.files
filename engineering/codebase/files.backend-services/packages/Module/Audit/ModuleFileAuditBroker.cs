using System.Globalization;
using Microsoft.EntityFrameworkCore;
using WoW.Two.Sdk.Beta.Be.Files.Audit;
using WoW.Two.Sdk.Beta.Be.Files.Module.Blobs;
using WoW.Two.Sdk.Beta.Be.Files.Module.Callers;
using WoW.Two.Sdk.Beta.Be.Files.Module.Persistence;
using WoW.Two.Sdk.Beta.Be.Files.Module.Store;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Audit;

/// <summary>Integrates the in-process audit trail of the caller's tenant.</summary>
internal sealed class ModuleFileAuditBroker(FilesDbContext db, IFilesCallerService callers) : IFileAuditBroker
{
    /// <inheritdoc />
    public async Task<Result<FileAuditPage>> ListAsync(FileAuditQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var tenantId = callers.Current.TenantId;
        if (!FileBlobKeyMapper.IsValidTenant(tenantId))
            return Result<FileAuditPage>.Fail(FilesErrors.TenantInvalid());

        var rows = db.Audit.AsNoTracking().Where(entry => entry.TenantId == tenantId);
        if (query.Cursor is not null)
        {
            if (!long.TryParse(query.Cursor, NumberStyles.None, CultureInfo.InvariantCulture, out var before))
                return Result<FileAuditPage>.Fail(FilesErrors.CursorInvalid());
            rows = rows.Where(entry => entry.Seq < before);
        }
        if (query.FileId is { } fileId)
        {
            var rowId = FileEntityMapper.ToRowId(fileId);
            rows = rows.Where(entry => entry.FileId == rowId);
        }
        if (query.Action is { } action)
            rows = rows.Where(entry => entry.Action == action.ToString());

        var limit = Math.Clamp(query.Limit, 1, FilesConstants.MaxPageSize);
        var page = await rows.OrderByDescending(entry => entry.Seq).Take(limit + 1).ToListAsync(cancellationToken);
        var next = page.Count > limit ? page[limit - 1].Seq.ToString(CultureInfo.InvariantCulture) : null;

        return Result<FileAuditPage>.Ok(new FileAuditPage(page.Take(limit).Select(ToModel).ToList(), next));
    }

    private static FileAuditEntryModel ToModel(FileAuditEntity entry) => new()
    {
        Sequence = entry.Seq,
        FileId = entry.FileId is null ? null : new FileId(Guid.Parse(entry.FileId)),
        Action = Enum.Parse<FileAuditAction>(entry.Action),
        Outcome = Enum.Parse<FileAuditOutcome>(entry.Outcome),
        ErrorCode = entry.ErrorCode,
        SubjectId = entry.SubjectId,
        AppId = entry.AppId,
        Bytes = entry.Bytes,
        OccurredAtUtc = entry.OccurredAtUtc,
    };
}
