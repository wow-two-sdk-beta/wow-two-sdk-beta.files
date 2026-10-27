using Microsoft.EntityFrameworkCore;
using WoW.Two.Sdk.Beta.Be.Files.Module.Blobs;
using WoW.Two.Sdk.Beta.Be.Files.Module.Persistence;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;
using WoW.Two.Sdk.Beta.Be.Storage.Core;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Store;

/// <summary>Opens a stored file's bytes from the blob store, whole or a range, for the store broker and delivery.</summary>
internal sealed class FileContentReader(FilesDbContext db, IBlobRepository blobs)
{
    /// <summary>Opens a ready file's bytes.</summary>
    /// <param name="row">The file.</param>
    /// <param name="range">The bytes to read; the end is clamped to the file; none reads it whole.</param>
    /// <param name="cancellationToken">The token that cancels the call.</param>
    /// <returns>The open content; <see cref="FilesErrors.FileNotReady"/>, <see cref="FilesErrors.RangeInvalid"/>, or
    /// <see cref="FilesErrors.FileNotFound"/> when the bytes are missing.</returns>
    public async Task<Result<FileContent>> OpenAsync(FileEntity row, ByteRange? range, CancellationToken cancellationToken)
    {
        if (row.State != nameof(FileState.Ready))
            return Result<FileContent>.Fail(FilesErrors.FileNotReady());

        var from = range?.From ?? 0;
        var to = Math.Min(range?.To ?? row.Size - 1, row.Size - 1);
        if (range is not null && (from < 0 || from >= row.Size || to < from))
            return Result<FileContent>.Fail(FilesErrors.RangeInvalid(row.Size));

        var key = await db.Blobs.AsNoTracking()
            .Where(blob => blob.TenantId == row.TenantId && blob.Sha256 == row.Sha256)
            .Select(blob => blob.StorageKey)
            .FirstOrDefaultAsync(cancellationToken) ?? FileBlobKeyMapper.ToKey(row.TenantId, row.Sha256);
        if (await blobs.OpenReadAsync(key, cancellationToken) is not { } bytes)
            return Result<FileContent>.Fail(FilesErrors.FileNotFound());

        var length = row.Size == 0 ? 0 : to - from + 1;
        var content = await RangeReadStream.OpenAsync(bytes, from, length, cancellationToken);
        return Result<FileContent>.Ok(new FileContent(content, row.ContentType, length, row.Size, range is null ? null : new ByteRange(from, to)));
    }
}
