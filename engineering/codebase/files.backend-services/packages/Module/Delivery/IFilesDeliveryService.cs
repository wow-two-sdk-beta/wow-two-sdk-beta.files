using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Delivery;

/// <summary>Defines what the content host serves: the file a signed or public URL names, then its bytes.</summary>
/// <remarks>No caller is involved: a signed URL carries its own authorization, and a public URL needs none.</remarks>
public interface IFilesDeliveryService
{
    /// <summary>Finds the file a signed URL names, when its signature holds and it has not expired.</summary>
    /// <param name="tenantId">The tenant in the URL's path; none for the default tenant.</param>
    /// <param name="id">The file id in the URL's path.</param>
    /// <param name="expires">The <c>exp</c> parameter.</param>
    /// <param name="disposition">The <c>d</c> parameter.</param>
    /// <param name="signature">The <c>sig</c> parameter.</param>
    /// <param name="cancellationToken">The token that cancels the call.</param>
    /// <returns>The file; <see cref="FilesErrors.UrlInvalid"/> for a bad or expired URL; not found once the file is gone.</returns>
    Task<Result<DeliverableFileModel>> FindSignedAsync(string? tenantId, FileId id, long expires, string? disposition, string? signature, CancellationToken cancellationToken = default);

    /// <summary>Finds a public, ready file with these bytes.</summary>
    /// <param name="tenantId">The tenant in the URL's path; none for the default tenant.</param>
    /// <param name="sha256">The SHA-256 in the URL's path.</param>
    /// <param name="cancellationToken">The token that cancels the call.</param>
    /// <returns>The file; not found when no public file of the tenant has the bytes.</returns>
    Task<Result<DeliverableFileModel>> FindPublicAsync(string? tenantId, string sha256, CancellationToken cancellationToken = default);

    /// <summary>Opens a found file's bytes, whole or a range.</summary>
    /// <param name="file">The file a find returned.</param>
    /// <param name="range">The bytes to read; none reads it whole.</param>
    /// <param name="cancellationToken">The token that cancels the call.</param>
    /// <returns>The open content.</returns>
    Task<Result<FileContent>> OpenAsync(DeliverableFileModel file, ByteRange? range, CancellationToken cancellationToken = default);
}
