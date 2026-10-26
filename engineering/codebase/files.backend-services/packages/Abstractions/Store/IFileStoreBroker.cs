using WoW.Two.Sdk.Beta.Be.Foundation.Results;

namespace WoW.Two.Sdk.Beta.Be.Files.Store;

/// <summary>Integrates the Files store — read, list, change, delete and restore files — in process or over the service.</summary>
/// <remarks>Every call acts for the current principal and sees only its tenant's files.</remarks>
public interface IFileStoreBroker
{
    /// <summary>Gets a file's metadata.</summary>
    /// <param name="id">The file.</param>
    /// <param name="cancellationToken">The token that cancels the call.</param>
    /// <returns>The file; <see cref="FilesErrors.FileNotFound"/> when the tenant holds no such file.</returns>
    Task<Result<FileModel>> GetAsync(FileId id, CancellationToken cancellationToken = default);

    /// <summary>Opens a file's bytes, whole or a range.</summary>
    /// <param name="id">The file.</param>
    /// <param name="range">The bytes to read; the whole file when none.</param>
    /// <param name="cancellationToken">The token that cancels the call.</param>
    /// <returns>The open content; <see cref="FilesErrors.FileNotReady"/> before the file is ready.</returns>
    Task<Result<FileContent>> OpenReadAsync(FileId id, ByteRange? range = null, CancellationToken cancellationToken = default);

    /// <summary>Lists the tenant's files, newest first.</summary>
    /// <param name="query">The filters and the page.</param>
    /// <param name="cancellationToken">The token that cancels the call.</param>
    /// <returns>One page of files.</returns>
    Task<Result<FilePage>> ListAsync(FileQuery query, CancellationToken cancellationToken = default);

    /// <summary>Changes a file's name, visibility or expiry.</summary>
    /// <param name="id">The file.</param>
    /// <param name="patch">The fields to change.</param>
    /// <param name="cancellationToken">The token that cancels the call.</param>
    /// <returns>The changed file.</returns>
    Task<Result<FileModel>> UpdateAsync(FileId id, FilePatch patch, CancellationToken cancellationToken = default);

    /// <summary>Deletes a file softly; it stays restorable until its retention ends, then it is purged.</summary>
    /// <param name="id">The file.</param>
    /// <param name="cancellationToken">The token that cancels the call.</param>
    /// <returns>The deleted file.</returns>
    Task<Result<FileModel>> DeleteAsync(FileId id, CancellationToken cancellationToken = default);

    /// <summary>Restores a deleted file within its retention.</summary>
    /// <param name="id">The file.</param>
    /// <param name="cancellationToken">The token that cancels the call.</param>
    /// <returns>The restored file.</returns>
    Task<Result<FileModel>> RestoreAsync(FileId id, CancellationToken cancellationToken = default);
}
