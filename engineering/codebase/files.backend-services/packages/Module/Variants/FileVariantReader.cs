using Microsoft.EntityFrameworkCore;
using WoW.Two.Sdk.Beta.Be.Files.Module.Persistence;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Variants;

/// <summary>Reads files' variant rows, for the models the brokers return and the URLs they issue.</summary>
internal sealed class FileVariantReader(FilesDbContext db)
{
    /// <summary>Lists one file's variants, narrowest first.</summary>
    /// <param name="tenantId">The file's tenant.</param>
    /// <param name="fileId">The file's row id.</param>
    /// <param name="cancellationToken">The token that cancels the call.</param>
    public Task<List<FileVariantEntity>> ListAsync(string tenantId, string fileId, CancellationToken cancellationToken) =>
        db.Variants.AsNoTracking()
            .Where(variant => variant.TenantId == tenantId && variant.FileId == fileId)
            .OrderBy(variant => variant.Width)
            .ToListAsync(cancellationToken);

    /// <summary>Lists several files' variants, by file row id.</summary>
    /// <param name="tenantId">The files' tenant.</param>
    /// <param name="fileIds">The files' row ids.</param>
    /// <param name="cancellationToken">The token that cancels the call.</param>
    public async Task<ILookup<string, FileVariantEntity>> ListAsync(string tenantId, IReadOnlyCollection<string> fileIds, CancellationToken cancellationToken)
    {
        if (fileIds.Count == 0)
            return Array.Empty<FileVariantEntity>().ToLookup(variant => variant.FileId);

        var rows = await db.Variants.AsNoTracking()
            .Where(variant => variant.TenantId == tenantId && fileIds.Contains(variant.FileId))
            .ToListAsync(cancellationToken);
        return rows.ToLookup(variant => variant.FileId);
    }
}
