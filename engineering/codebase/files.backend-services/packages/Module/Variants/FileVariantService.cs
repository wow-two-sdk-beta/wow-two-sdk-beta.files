using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WoW.Two.Sdk.Beta.Be.Files.Formats.Types;
using WoW.Two.Sdk.Beta.Be.Files.Module.Blobs;
using WoW.Two.Sdk.Beta.Be.Files.Module.Persistence;
using WoW.Two.Sdk.Beta.Be.Files.Module.Policies;
using WoW.Two.Sdk.Beta.Be.Files.Module.Store;
using WoW.Two.Sdk.Beta.Be.Media.Images;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Variants;

/// <summary>Makes a stored image's variants: one WebP per policy width no wider than the source, upright, without
/// metadata, keeping the aspect ratio; each stored once per tenant under its own content-hash key, with its row.</summary>
/// <remarks>A variant that fails — unreadable bytes, an image over the image service's limits, a store error — is
/// logged and skipped; it never fails the upload.</remarks>
internal sealed class FileVariantService(
    FilesDbContext db,
    FileBlobWriter writer,
    FileBlobLocks locks,
    IImageService images,
    IOptionsMonitor<ImageOptions> imageOptions,
    ILogger<FileVariantService> logger)
{
    /// <summary>Makes and records the variants of a stored file.</summary>
    /// <param name="file">The saved file row.</param>
    /// <param name="type">The registry's type of the file.</param>
    /// <param name="open">Opens the file's bytes from their start; each call returns a stream the caller disposes.</param>
    /// <param name="policy">The app policy, which names the widths and the quality.</param>
    /// <param name="now">The current time.</param>
    /// <param name="cancellationToken">The token that cancels the call.</param>
    /// <returns>The variants made, narrowest first; empty when the file is no raster image, the policy names no width,
    /// or every variant failed.</returns>
    public async Task<IReadOnlyList<FileVariantEntity>> CreateAsync(FileEntity file, FileTypeModel type, Func<Stream> open, FilePolicyOptions policy, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (policy.Variants.Count == 0 || !FileVariantWidthMapper.IsSource(type))
            return [];

        if (file.Size > imageOptions.CurrentValue.MaxInputBytes)
        {
            logger.LogWarning("File {FileId} is over the image input limit; it gets no variants.", file.Id);
            return [];
        }

        int sourceWidth;
        try
        {
            await using var source = open();
            sourceWidth = (await images.ProbeAsync(source, cancellationToken)).Width;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "File {FileId} could not be read as an image; it gets no variants.", file.Id);
            return [];
        }

        var created = new List<FileVariantEntity>();
        foreach (var width in FileVariantWidthMapper.Plan(sourceWidth, policy.Variants))
        {
            try
            {
                var variant = await CreateAsync(file, width, open, policy.VariantQuality, now, created, cancellationToken);
                if (variant is not null)
                    created.Add(variant);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                DetachPending();
                logger.LogWarning(exception, "File {FileId}: the {Width} px variant failed and was skipped.", file.Id, width);
            }
        }

        return created;
    }

    /// <summary>Makes one variant and saves its bytes and row under the bytes' lock.</summary>
    private async Task<FileVariantEntity?> CreateAsync(FileEntity file, int width, Func<Stream> open, int quality, DateTimeOffset now, List<FileVariantEntity> created, CancellationToken cancellationToken)
    {
        ImageResult result;
        await using (var source = open())
        {
            result = await images.EditAsync(source, new ImageEditSpec
            {
                Resize = new ImageResizeSpec { Width = width },
                Output = new ImageOutputSpec { Format = ImageFormat.Webp, Quality = quality },
            }, cancellationToken);
        }

        if (created.Exists(variant => variant.Width == result.Width))
            return null;

        var row = new FileVariantEntity
        {
            TenantId = file.TenantId,
            FileId = file.Id,
            Width = result.Width,
            Height = result.Height,
            ContentType = FileTypeMapper.Default.FromMimeType(result.ContentType)?.MimeType ?? result.ContentType,
            Size = result.Content.LongLength,
            Sha256 = Convert.ToHexStringLower(SHA256.HashData(result.Content)),
            CreatedAtUtc = now,
        };

        using (await locks.AcquireAsync(row.Sha256, cancellationToken))
        {
            await writer.StoreAsync(file.TenantId, row.Sha256, row.Size, () => new MemoryStream(result.Content, writable: false), row.ContentType, now, cancellationToken);
            db.Variants.Add(row);
            await db.SaveChangesAsync(cancellationToken);
        }

        return row;
    }

    /// <summary>Drops a failed variant's unsaved rows, so the next save does not retry them.</summary>
    private void DetachPending()
    {
        foreach (var entry in db.ChangeTracker.Entries().Where(entry => entry.State is EntityState.Added or EntityState.Modified).ToList())
            entry.State = EntityState.Detached;
    }
}
