using Microsoft.EntityFrameworkCore;
using WoW.Two.Sdk.Beta.Be.Files.Module.Blobs;
using WoW.Two.Sdk.Beta.Be.Files.Module.Persistence;
using WoW.Two.Sdk.Beta.Be.Storage.Core;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Sweeps;

/// <summary>Purges file rows past their purge or expiry time, then the bytes no row uses that no upload touched in
/// the last hour; each blob is deleted under its lock, so a concurrent upload reusing it wins.</summary>
internal sealed class FilesSweepService(FilesDbContext db, IBlobRepository blobs, FileBlobLocks locks, TimeProvider time) : IFilesSweepService
{
    /// <summary>Holds how long unused bytes are kept after their last upload, covering uploads still in flight.</summary>
    internal static readonly TimeSpan BlobGrace = TimeSpan.FromHours(1);

    /// <inheritdoc />
    public async Task<FileSweepResultModel> SweepAsync(CancellationToken cancellationToken = default)
    {
        var now = time.GetUtcNow();
        var files = await db.Files
            .Where(file => (file.PurgeAtUtc != null && file.PurgeAtUtc <= now) || (file.ExpiresAtUtc != null && file.ExpiresAtUtc <= now))
            .ExecuteDeleteAsync(cancellationToken);

        var cutoff = now - BlobGrace;
        var unused = await Unused(cutoff).AsNoTracking().ToListAsync(cancellationToken);
        var purged = 0;
        foreach (var blob in unused)
        {
            using (await locks.AcquireAsync(blob.Sha256, cancellationToken))
            {
                var deleted = await Unused(cutoff)
                    .Where(row => row.TenantId == blob.TenantId && row.Sha256 == blob.Sha256)
                    .ExecuteDeleteAsync(cancellationToken);
                if (deleted == 0)
                    continue;

                await blobs.DeleteAsync(blob.StorageKey, cancellationToken);
                purged++;
            }
        }

        return new FileSweepResultModel(files, purged);
    }

    /// <summary>Gets the blobs no file row uses and no upload touched since <paramref name="cutoff"/>.</summary>
    private IQueryable<FileBlobEntity> Unused(DateTimeOffset cutoff) =>
        db.Blobs.Where(blob => blob.LastUsedAtUtc < cutoff
            && !db.Files.Any(file => file.TenantId == blob.TenantId && file.Sha256 == blob.Sha256));
}
