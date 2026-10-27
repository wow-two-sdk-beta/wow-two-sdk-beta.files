using WoW.Two.Sdk.Beta.Be.Files.Client.Transport;
using WoW.Two.Sdk.Beta.Be.Files.References;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;

namespace WoW.Two.Sdk.Beta.Be.Files.Client.Files;

/// <summary>Integrates the running Files service's references.</summary>
internal sealed class ApiFileReferencesBroker(FilesApiTransport transport) : IFileReferencesBroker
{
    /// <inheritdoc />
    public Task<Result> AttachAsync(FileId id, FileReference reference, CancellationToken cancellationToken = default) =>
        IsBlank(reference)
            ? Task.FromResult(Result.Fail(FilesErrors.ReferenceInvalid()))
            : transport.SendAsync(new HttpRequestMessage(HttpMethod.Put, FilesApiRouteMapper.Reference(id, reference)), cancellationToken);

    /// <inheritdoc />
    public Task<Result> DetachAsync(FileId id, FileReference reference, CancellationToken cancellationToken = default) =>
        IsBlank(reference)
            ? Task.FromResult(Result.Fail(FilesErrors.ReferenceInvalid()))
            : transport.SendAsync(new HttpRequestMessage(HttpMethod.Delete, FilesApiRouteMapper.Reference(id, reference)), cancellationToken);

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<FileReference>>> ListAsync(FileId id, CancellationToken cancellationToken = default) =>
        transport.SendAsync<IReadOnlyList<FileReference>>(new HttpRequestMessage(HttpMethod.Get, FilesApiRouteMapper.File(id, "references")), cancellationToken);

    /// <summary>Tells a reference no route can carry; the service refuses it the same way.</summary>
    private static bool IsBlank(FileReference reference) =>
        string.IsNullOrWhiteSpace(reference.RecordType) || string.IsNullOrWhiteSpace(reference.RecordId);
}
