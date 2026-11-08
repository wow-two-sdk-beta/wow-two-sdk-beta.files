using WoW.Two.Sdk.Beta.Be.Foundation.Results;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Imports;

/// <summary>Defines imports: a product's legacy files moving into a Files tenant, keyed by source and key, with the id map
/// the product's cutover reads.</summary>
/// <remarks>The caller is checked by the host: the operator opens imports and mints tokens; an import's token pushes into
/// it and reads it.</remarks>
public interface IFilesImportsService
{
    /// <summary>Opens an import and issues its first token.</summary>
    /// <param name="spec">The tenant and the sources.</param>
    /// <param name="cancellationToken">The token that cancels the call.</param>
    /// <returns>The import's id and token.</returns>
    Task<Result<FileImportTicketModel>> OpenAsync(FileImportSpec spec, CancellationToken cancellationToken = default);

    /// <summary>Issues a new token for an import.</summary>
    /// <param name="importId">The import.</param>
    /// <param name="cancellationToken">The token that cancels the call.</param>
    /// <returns>The token; <see cref="FileImportErrors.ImportNotFound"/> for an unknown import.</returns>
    Task<Result<FileImportTokenModel>> IssueTokenAsync(string importId, CancellationToken cancellationToken = default);

    /// <summary>Gets an import and each source's counts.</summary>
    /// <param name="importId">The import.</param>
    /// <param name="cancellationToken">The token that cancels the call.</param>
    /// <returns>The import; <see cref="FileImportErrors.ImportNotFound"/> for an unknown one.</returns>
    Task<Result<FileImportModel>> GetAsync(string importId, CancellationToken cancellationToken = default);

    /// <summary>Pages an import's id map in push order.</summary>
    /// <param name="importId">The import.</param>
    /// <param name="cursor">The previous page's <see cref="FileImportMapPage.Next"/>; none for the first page.</param>
    /// <param name="limit">The most items to return, 1 to <see cref="FileImportConstants.MaxMapPageSize"/>.</param>
    /// <param name="cancellationToken">The token that cancels the call.</param>
    /// <returns>The page; <see cref="FileImportErrors.ImportNotFound"/> for an unknown import.</returns>
    Task<Result<FileImportMapPage>> MapAsync(string importId, string? cursor, int limit, CancellationToken cancellationToken = default);

    /// <summary>Pushes one file into an import: uploads it as <c>Private</c> in the import's tenant as <c>import:{id}</c>,
    /// attaches its reference and records the key. Idempotent per (import, source, key).</summary>
    /// <param name="importId">The import.</param>
    /// <param name="content">The bytes; not read when the key is already there.</param>
    /// <param name="spec">The source, key, SHA-256 and the optional reference, name, type and app.</param>
    /// <param name="cancellationToken">The token that cancels the call.</param>
    /// <returns>The file, <see cref="FileImportPushResultModel.Created"/> false when the key was there with the same
    /// SHA-256; <see cref="FileImportErrors.KeyConflict"/> when it was there with another;
    /// <see cref="FileImportErrors.Sha256Mismatch"/> when the bytes contradict <c>sha256</c>, counted as failed.</returns>
    Task<Result<FileImportPushResultModel>> PushAsync(string importId, Stream content, FileImportPushSpec spec, CancellationToken cancellationToken = default);
}
