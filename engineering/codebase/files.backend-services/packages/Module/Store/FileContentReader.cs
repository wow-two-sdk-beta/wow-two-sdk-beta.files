using Microsoft.EntityFrameworkCore;
using WoW.Two.Sdk.Beta.Be.Files.Module.Blobs;
using WoW.Two.Sdk.Beta.Be.Files.Module.Persistence;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;
using WoW.Two.Sdk.Beta.Be.Storage.Core;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Store;

/// <summary>Opens a stored file's bytes, or a variant's, from the blob store, whole or a range, for the store broker and
/// delivery.</summary>
internal sealed class FileContentReader(FilesDbContext db, IBlobRepository blobs)
{
    /// <summary>Opens a ready file's bytes.</summary>
    /// <param name="row">The file.</param>
    /// <param name="range">The bytes to read; the end is clamped to the file; none reads it whole.</param>
    /// <param name="cancellationToken">The token that cancels the call.</param>
    /// <returns>The open content; <see cref="FilesErrors.FileNotReady"/>, <see cref="FilesErrors.RangeInvalid"/>, or
    /// <see cref="FilesErrors.FileNotFound"/> when the bytes are missing.</returns>
    public Task<Result<FileContent>> OpenAsync(FileEntity row, ByteRange? range, CancellationToken cancellationToken) =>
        row.State != FileState.Ready
            ? Task.FromResult(Result<FileContent>.Fail(FilesErrors.FileNotReady()))
            : OpenAsync(row.TenantId, row.Sha256, row.Size, row.ContentType, range, cancellationToken);

    /// <summary>Opens a ready file's variant; the caller has checked the source file is ready.</summary>
    /// <param name="variant">The variant.</param>
    /// <param name="range">The bytes to read; the end is clamped to the variant; none reads it whole.</param>
    /// <param name="cancellationToken">The token that cancels the call.</param>
    /// <returns>The open content; <see cref="FilesErrors.RangeInvalid"/>, or <see cref="FilesErrors.FileNotFound"/> when
    /// the bytes are missing.</returns>
    public Task<Result<FileContent>> OpenAsync(FileVariantEntity variant, ByteRange? range, CancellationToken cancellationToken) =>
        OpenAsync(variant.TenantId, variant.Sha256, variant.Size, variant.ContentType, range, cancellationToken);

    private async Task<Result<FileContent>> OpenAsync(string tenantId, string sha256, long size, string contentType, ByteRange? range, CancellationToken cancellationToken)
    {
        var from = range?.From ?? 0;
        var to = Math.Min(range?.To ?? size - 1, size - 1);
        if (range is not null && (from < 0 || from >= size || to < from))
            return Result<FileContent>.Fail(FilesErrors.RangeInvalid(size));

        var key = await db.Blobs.AsNoTracking()
            .Where(blob => blob.TenantId == tenantId && blob.Sha256 == sha256)
            .Select(blob => blob.StorageKey)
            .FirstOrDefaultAsync(cancellationToken) ?? FileBlobKeyMapper.ToKey(tenantId, sha256);
        if (await blobs.OpenReadAsync(key, cancellationToken) is not { } bytes)
            return Result<FileContent>.Fail(FilesErrors.FileNotFound());

        var length = size == 0 ? 0 : to - from + 1;
        var content = await RangeReadStream.OpenAsync(bytes, from, length, cancellationToken);
        return Result<FileContent>.Ok(new FileContent(content, contentType, length, size, range is null ? null : new ByteRange(from, to)));
    }
}
