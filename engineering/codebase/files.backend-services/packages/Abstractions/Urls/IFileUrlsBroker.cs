using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;

namespace WoW.Two.Sdk.Beta.Be.Files.Urls;

/// <summary>Integrates Files URLs — signed links to private files, stable links to public ones — in process or over the service.</summary>
/// <remarks>URLs point at the tenant's content host, never at an app's origin.</remarks>
public interface IFileUrlsBroker
{
    /// <summary>Issues a short-lived signed URL to a file the caller may read.</summary>
    /// <param name="id">The file.</param>
    /// <param name="spec">The lifetime, disposition and the width that picks a variant.</param>
    /// <param name="cancellationToken">The token that cancels the call.</param>
    /// <returns>The signed URL; <see cref="FilesErrors.FileNotReady"/> before the file is ready; a validation failure
    /// for a width below 1.</returns>
    Task<Result<SignedFileUrl>> IssueAsync(FileId id, UrlSpec spec, CancellationToken cancellationToken = default);

    /// <summary>Gets the immutable public URL of a public file, or of one of its variants.</summary>
    /// <param name="id">The file.</param>
    /// <param name="width">The width in pixels the file is shown at: the narrowest variant at least that wide, else the
    /// widest; the original when none, or when the file has no variants.</param>
    /// <param name="cancellationToken">The token that cancels the call.</param>
    /// <returns>The URL; a forbidden failure when the file is not public; a validation failure for a width below 1.</returns>
    Task<Result<Uri>> GetPublicUrlAsync(FileId id, int? width = null, CancellationToken cancellationToken = default);
}
