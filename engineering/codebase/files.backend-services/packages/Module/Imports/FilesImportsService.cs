using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WoW.Two.Sdk.Beta.Be.Files.Module.Blobs;
using WoW.Two.Sdk.Beta.Be.Files.Module.Callers;
using WoW.Two.Sdk.Beta.Be.Files.Module.Persistence;
using WoW.Two.Sdk.Beta.Be.Files.Module.Uploads;
using WoW.Two.Sdk.Beta.Be.Files.References;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Files.Uploads;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;
using WoW.Two.Sdk.Beta.Be.Identity.CurrentPrincipal;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Imports;

/// <summary>Integrates imports over the module's store: each push checks the bytes against the mover's SHA-256, then
/// uploads them through <see cref="IFileUploadsBroker"/> as the import (tenant, subject <c>import:{id}</c>), so dedup,
/// type checks, policy and audit run as for any upload.</summary>
internal sealed partial class FilesImportsService(
    FilesDbContext db,
    IFileUploadsBroker uploads,
    IFileReferencesBroker references,
    IFileImportTokenService tokens,
    FilesCallerOverride callers,
    IOptions<FilesModuleOptions> options,
    TimeProvider time) : IFilesImportsService
{
    /// <inheritdoc />
    public async Task<Result<FileImportTicketModel>> OpenAsync(FileImportSpec spec, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(spec);

        if (!FileBlobKeyMapper.IsValidTenant(spec.Tenant))
            return Result<FileImportTicketModel>.Fail(FileImportErrors.TenantInvalid());
        if (spec.Sources is not { Count: > 0 and <= FileImportConstants.MaxSources } sources
            || sources.Any(source => source is null || !SourceId().IsMatch(source))
            || sources.Distinct(StringComparer.Ordinal).Count() != sources.Count)
            return Result<FileImportTicketModel>.Fail(FileImportErrors.SourcesInvalid());

        var id = Guid.CreateVersion7().ToString("D");
        db.Imports.Add(new FileImportEntity { Id = id, TenantId = spec.Tenant, State = FileImportConstants.OpenState, CreatedAtUtc = time.GetUtcNow() });
        db.ImportSources.AddRange(sources.Select(source => new FileImportSourceEntity { ImportId = id, SourceId = source }));
        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();

        var token = tokens.Issue(id, spec.Tenant);
        return Result<FileImportTicketModel>.Ok(new FileImportTicketModel(id, token.Token, token.ExpiresAt));
    }

    /// <inheritdoc />
    public async Task<Result<FileImportTokenModel>> IssueTokenAsync(string importId, CancellationToken cancellationToken = default) =>
        await FindAsync(importId, cancellationToken) is { } import
            ? Result<FileImportTokenModel>.Ok(tokens.Issue(import.Id, import.TenantId))
            : Result<FileImportTokenModel>.Fail(FileImportErrors.ImportNotFound());

    /// <inheritdoc />
    public async Task<Result<FileImportModel>> GetAsync(string importId, CancellationToken cancellationToken = default)
    {
        if (await FindAsync(importId, cancellationToken) is not { } import)
            return Result<FileImportModel>.Fail(FileImportErrors.ImportNotFound());

        var sources = await db.ImportSources.AsNoTracking()
            .Where(source => source.ImportId == import.Id)
            .OrderBy(source => source.SourceId)
            .ToListAsync(cancellationToken);
        var moved = await db.ImportItems.AsNoTracking()
            .Where(item => item.ImportId == import.Id)
            .GroupBy(item => item.SourceId)
            .Select(group => new { Source = group.Key, Files = group.LongCount(), Bytes = group.Sum(item => item.Size) })
            .ToDictionaryAsync(row => row.Source, StringComparer.Ordinal, cancellationToken);

        var models = sources.Select(source => moved.TryGetValue(source.SourceId, out var counts)
            ? new FileImportSourceModel(source.SourceId, counts.Files, counts.Bytes, source.Failed)
            : new FileImportSourceModel(source.SourceId, 0, 0, source.Failed)).ToList();
        return Result<FileImportModel>.Ok(new FileImportModel(import.Id, import.TenantId, import.State, models));
    }

    /// <inheritdoc />
    public async Task<Result<FileImportMapPage>> MapAsync(string importId, string? cursor, int limit, CancellationToken cancellationToken = default)
    {
        long after = 0;
        if (cursor is { Length: > 0 } && !long.TryParse(cursor, NumberStyles.None, CultureInfo.InvariantCulture, out after))
            return Result<FileImportMapPage>.Fail(FileImportErrors.CursorInvalid());
        if (await FindAsync(importId, cancellationToken) is not { } import)
            return Result<FileImportMapPage>.Fail(FileImportErrors.ImportNotFound());

        var size = Math.Clamp(limit, 1, FileImportConstants.MaxMapPageSize);
        var page = await db.ImportItems.AsNoTracking()
            .Where(item => item.ImportId == import.Id && item.Seq > after)
            .OrderBy(item => item.Seq)
            .Take(size + 1)
            .ToListAsync(cancellationToken);
        var next = page.Count > size ? page[size - 1].Seq.ToString(CultureInfo.InvariantCulture) : null;

        return Result<FileImportMapPage>.Ok(new FileImportMapPage(page.Take(size).Select(ToModel).ToList(), next));
    }

    /// <inheritdoc />
    public async Task<Result<FileImportPushResultModel>> PushAsync(string importId, Stream content, FileImportPushSpec spec, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(spec);

        if (await FindAsync(importId, cancellationToken) is not { } import)
            return Fail(FileImportErrors.ImportNotFound());
        if (string.IsNullOrEmpty(spec.Source)
            || !await db.ImportSources.AnyAsync(source => source.ImportId == import.Id && source.SourceId == spec.Source, cancellationToken))
            return Fail(FileImportErrors.SourceUnknown());
        if (string.IsNullOrWhiteSpace(spec.Key) || spec.Key.Length > FileImportConstants.MaxKeyLength)
            return Fail(FileImportErrors.KeyInvalid());
        if (spec.Sha256 is null || !Sha256Hex().IsMatch(spec.Sha256))
            return Fail(FileImportErrors.Sha256Invalid());

        FileReference? reference = null;
        if (spec.Reference is { Length: > 0 } text)
        {
            if (ToReference(text) is not { } parsed)
                return Fail(FileImportErrors.ReferenceInvalid());
            reference = parsed;
        }

        // The key already moved: the same bytes answer with the stored file, other bytes conflict; the body stays unread.
        if (await FindItemAsync(import.Id, spec.Source, spec.Key, cancellationToken) is { } earlier)
            return Again(earlier, spec.Sha256);

        var app = spec.App is { Length: > 0 } named ? named : AppOf(spec.Reference);
        var spooled = await UploadSpool.CreateAsync(content, options.Value.PolicyFor(app).MaxBytes, options.Value.TempPath ?? Path.GetTempPath(), cancellationToken);
        if (spooled is Result<UploadSpool>.Failure tooLarge)
            return await FailedAsync(import.Id, spec.Source, tooLarge.Error, cancellationToken);

        await using var spool = ((Result<UploadSpool>.Success)spooled).Value;
        if (!string.Equals(spool.Sha256, spec.Sha256, StringComparison.Ordinal))
            return await FailedAsync(import.Id, spec.Source, FileImportErrors.Sha256Mismatch(), cancellationToken);

        FileModel file;
        using (callers.Use(CallerOf(import, app)))
        {
            var uploaded = await uploads.UploadAsync(spool.OpenRead(), new UploadSpec
            {
                Name = spec.Name is { Length: > 0 } name ? name : spec.Key[(spec.Key.LastIndexOf('/') + 1)..],
                ContentType = spec.ContentType is { Length: > 0 } type ? type : null,
                AppId = app,
                Visibility = FileVisibility.Private,
                IdempotencyKey = $"{FileImportConstants.SubjectPrefix}{import.Id}:{spec.Source}:{spec.Key}",
            }, cancellationToken);
            if (uploaded is Result<FileModel>.Failure refused)
                return await FailedAsync(import.Id, spec.Source, refused.Error, cancellationToken);

            file = ((Result<FileModel>.Success)uploaded).Value;

            // A retry after a lost answer finds the first upload by its key; bytes other than that upload's conflict.
            if (!string.Equals(file.Sha256, spec.Sha256, StringComparison.Ordinal))
                return Fail(FileImportErrors.KeyConflict());

            if (reference is { } attach && await references.AttachAsync(file.Id, attach, cancellationToken) is Result.Failure unattached)
                return Fail(unattached.Error);
        }

        db.ChangeTracker.Clear();
        db.ImportItems.Add(new FileImportItemEntity
        {
            ImportId = import.Id,
            SourceId = spec.Source,
            SourceKey = spec.Key,
            FileId = FileIdText(file.Id),
            Sha256 = file.Sha256,
            Size = file.Size,
            Reference = spec.Reference is { Length: > 0 } ? spec.Reference : null,
            CreatedAtUtc = time.GetUtcNow(),
        });
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // A concurrent push of the same key won the insert; answer as a repeat of it.
            db.ChangeTracker.Clear();
            if (await FindItemAsync(import.Id, spec.Source, spec.Key, cancellationToken) is not { } raced)
                throw;
            return Again(raced, spec.Sha256);
        }

        db.ChangeTracker.Clear();
        return Result<FileImportPushResultModel>.Ok(new FileImportPushResultModel(file.Id, file.Sha256, file.Size, Created: true));
    }

    /// <summary>Answers a push of a key already moved.</summary>
    private static Result<FileImportPushResultModel> Again(FileImportItemEntity item, string sha256) =>
        string.Equals(item.Sha256, sha256, StringComparison.Ordinal)
            ? Result<FileImportPushResultModel>.Ok(new FileImportPushResultModel(ToFileId(item.FileId), item.Sha256, item.Size, Created: false))
            : Fail(FileImportErrors.KeyConflict());

    /// <summary>Counts a refused push against its source and answers with the error.</summary>
    private async Task<Result<FileImportPushResultModel>> FailedAsync(string importId, string sourceId, AppError error, CancellationToken cancellationToken)
    {
        await db.ImportSources
            .Where(source => source.ImportId == importId && source.SourceId == sourceId)
            .ExecuteUpdateAsync(set => set.SetProperty(source => source.Failed, source => source.Failed + 1), cancellationToken);
        return Fail(error);
    }

    private Task<FileImportEntity?> FindAsync(string importId, CancellationToken cancellationToken) =>
        string.IsNullOrEmpty(importId)
            ? Task.FromResult<FileImportEntity?>(null)
            : db.Imports.AsNoTracking().FirstOrDefaultAsync(import => import.Id == importId, cancellationToken);

    private Task<FileImportItemEntity?> FindItemAsync(string importId, string sourceId, string key, CancellationToken cancellationToken) =>
        db.ImportItems.AsNoTracking().FirstOrDefaultAsync(
            item => item.ImportId == importId && item.SourceId == sourceId && item.SourceKey == key, cancellationToken);

    /// <summary>Builds the caller an import's push acts as: its tenant, subject <c>import:{id}</c>, the push's app.</summary>
    private static FilesCallerModel CallerOf(FileImportEntity import, string? app) => new()
    {
        TenantId = import.TenantId,
        Kind = PrincipalKind.Service,
        SubjectId = FileImportConstants.SubjectPrefix + import.Id,
        AppId = app,
    };

    /// <summary>Reads <c>{recordType}:{recordId}</c>, split at the first <c>:</c>.</summary>
    private static FileReference? ToReference(string text)
    {
        var colon = text.IndexOf(':', StringComparison.Ordinal);
        if (colon <= 0 || colon == text.Length - 1)
            return null;

        var reference = new FileReference(text[..colon], text[(colon + 1)..]);
        return reference.RecordType.Length <= FilesConstants.MaxReferenceLength && reference.RecordId.Length <= FilesConstants.MaxReferenceLength
            && !string.IsNullOrWhiteSpace(reference.RecordType) && !string.IsNullOrWhiteSpace(reference.RecordId)
                ? reference
                : null;
    }

    /// <summary>Takes the app from a reference: its text before the first <c>.</c>; none without one.</summary>
    private static string? AppOf(string? reference)
    {
        if (reference is null)
            return null;

        var dot = reference.IndexOf('.', StringComparison.Ordinal);
        var colon = reference.IndexOf(':', StringComparison.Ordinal);
        return dot > 0 && (colon < 0 || dot < colon) ? reference[..dot] : null;
    }

    private static FileImportItemModel ToModel(FileImportItemEntity item) =>
        new(item.SourceId, item.SourceKey, item.Reference, ToFileId(item.FileId), item.Sha256, item.Size);

    private static FileId ToFileId(string text) => new(Guid.Parse(text));

    private static string FileIdText(FileId id) => id.Value.ToString("D");

    private static Result<FileImportPushResultModel> Fail(AppError error) => Result<FileImportPushResultModel>.Fail(error);

    [GeneratedRegex(FileImportConstants.SourceIdPattern)]
    private static partial Regex SourceId();

    [GeneratedRegex("^[0-9a-f]{64}$")]
    private static partial Regex Sha256Hex();
}
