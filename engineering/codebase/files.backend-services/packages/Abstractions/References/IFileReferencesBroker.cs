using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;

namespace WoW.Two.Sdk.Beta.Be.Files.References;

/// <summary>Integrates Files references — which records use a file — in process or over the service.</summary>
/// <remarks>A file with no reference and no expiry is an orphan; the sweep purges it after retention.</remarks>
public interface IFileReferencesBroker
{
    /// <summary>Attaches a file to a record; attaching twice is a no-op.</summary>
    /// <param name="id">The file.</param>
    /// <param name="reference">The record that uses it.</param>
    /// <param name="cancellationToken">The token that cancels the call.</param>
    /// <returns>Success; <see cref="FilesErrors.FileNotFound"/> when the tenant holds no such file.</returns>
    Task<Result> AttachAsync(FileId id, FileReference reference, CancellationToken cancellationToken = default);

    /// <summary>Detaches a file from a record.</summary>
    /// <param name="id">The file.</param>
    /// <param name="reference">The record that no longer uses it.</param>
    /// <param name="cancellationToken">The token that cancels the call.</param>
    /// <returns>Success; <see cref="FilesErrors.ReferenceNotFound"/> when it was not attached.</returns>
    Task<Result> DetachAsync(FileId id, FileReference reference, CancellationToken cancellationToken = default);

    /// <summary>Lists the records that use a file.</summary>
    /// <param name="id">The file.</param>
    /// <param name="cancellationToken">The token that cancels the call.</param>
    /// <returns>The references, oldest first.</returns>
    Task<Result<IReadOnlyList<FileReference>>> ListAsync(FileId id, CancellationToken cancellationToken = default);
}
