using Files.Application.Files.Queries;
using Files.Infrastructure.Access.Extensions;
using WoW.Two.Sdk.Beta.Be.Files.Module.Callers;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace Files.Infrastructure.Files.QueryHandlers;

/// <inheritdoc />
public sealed class GetFileByIdQueryHandler(IFileStoreBroker store, FilesCallerOverride callers)
    : IQueryHandler<GetFileByIdQuery, AppResult<FileModel>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<FileModel>> HandleAsync(GetFileByIdQuery request, CancellationToken ct)
    {
        using var caller = callers.Use(request.Actor);
        var result = await store.GetAsync(request.Id, ct);

        return result.Match(value => AppResult<FileModel>.Ok(value), error => AppResult<FileModel>.Fail(error));
    }
}
