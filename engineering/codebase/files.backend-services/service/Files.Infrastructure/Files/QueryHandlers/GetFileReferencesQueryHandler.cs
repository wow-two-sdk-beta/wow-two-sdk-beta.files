using Files.Application.Files.Queries;
using Files.Infrastructure.Access.Extensions;
using WoW.Two.Sdk.Beta.Be.Files.Module.Callers;
using WoW.Two.Sdk.Beta.Be.Files.References;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace Files.Infrastructure.Files.QueryHandlers;

/// <inheritdoc />
public sealed class GetFileReferencesQueryHandler(IFileReferencesBroker references, FilesCallerOverride callers)
    : IQueryHandler<GetFileReferencesQuery, AppResult<IReadOnlyList<FileReference>>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<IReadOnlyList<FileReference>>> HandleAsync(GetFileReferencesQuery request, CancellationToken ct)
    {
        using var caller = callers.Use(request.Actor);
        var result = await references.ListAsync(request.Id, ct);

        return result.Match(value => AppResult<IReadOnlyList<FileReference>>.Ok(value), error => AppResult<IReadOnlyList<FileReference>>.Fail(error));
    }
}
