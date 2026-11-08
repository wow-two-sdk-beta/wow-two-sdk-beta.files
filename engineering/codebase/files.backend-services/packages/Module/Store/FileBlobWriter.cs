using Microsoft.EntityFrameworkCore;
using WoW.Two.Sdk.Beta.Be.Files.Module.Blobs;
using WoW.Two.Sdk.Beta.Be.Files.Module.Persistence;
using WoW.Two.Sdk.Beta.Be.Storage.Core;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Store;

/// <summary>Stores a tenant's bytes once under their content-hash key: reuses the tenant's copy, or saves the bytes and
/// adds their <see cref="FileBlobEntity"/> to the pending save.</summary>
/// <remarks>Call it under the digest's <see cref="FileBlobLocks"/> lock, and save the context before releasing it, so
/// the sweep never deletes bytes a new row is about to use.</remarks>
internal sealed class FileBlobWriter(FilesDbContext db, IBlobRepository blobs)
{
    /// <summary>Reuses or stores the bytes.</summary>
    /// <param name="tenantId">The owning tenant.</param>
    /// <param name="sha256">The SHA-256 of the bytes, lowercase hex.</param>
    /// <param name="size">The size in bytes.</param>
    /// <param name="open">Opens the bytes from their start; called only when the tenant has no copy.</param>
    /// <param name="contentType">The content type the blob store records.</param>
    /// <param name="now">The current time; it marks the blob used.</param>
    /// <param name="cancellationToken">The token that cancels the call.</param>
    /// <returns>Whether the bytes were saved now, rather than reused.</returns>
    public async Task<bool> StoreAsync(string tenantId, string sha256, long size, Func<Stream> open, string contentType, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var reused = await db.Blobs
            .Where(blob => blob.TenantId == tenantId && blob.Sha256 == sha256)
            .ExecuteUpdateAsync(set => set.SetProperty(blob => blob.LastUsedAtUtc, now), cancellationToken);
        if (reused > 0)
            return false;

        var key = FileBlobKeyMapper.ToKey(tenantId, sha256);
        await using (var bytes = open())
            await blobs.SaveAsync(key, bytes, contentType, cancellationToken);

        db.Blobs.Add(new FileBlobEntity
        {
            TenantId = tenantId,
            Sha256 = sha256,
            StorageKey = key,
            Size = size,
            CreatedAtUtc = now,
            LastUsedAtUtc = now,
        });
        return true;
    }
}
