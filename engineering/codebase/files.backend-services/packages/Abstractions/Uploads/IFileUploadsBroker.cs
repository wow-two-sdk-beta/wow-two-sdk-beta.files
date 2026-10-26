using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;

namespace WoW.Two.Sdk.Beta.Be.Files.Uploads;

/// <summary>Integrates Files uploads — bytes in, checked against the app policy — in process or over the service.</summary>
public interface IFileUploadsBroker
{
    /// <summary>Uploads a file through the caller: checks size, type and magic bytes, stores the bytes once per tenant.</summary>
    /// <param name="content">The bytes, read to the end.</param>
    /// <param name="spec">The name, claimed type, app, visibility and idempotency key.</param>
    /// <param name="cancellationToken">The token that cancels the call.</param>
    /// <returns>The stored file; a validation failure from <see cref="FilesErrors"/> when the policy refuses it.</returns>
    Task<Result<FileModel>> UploadAsync(Stream content, UploadSpec spec, CancellationToken cancellationToken = default);
}
