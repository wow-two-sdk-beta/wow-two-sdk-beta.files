using Files.Application.Files.Queries;
using Files.Infrastructure.Access.Extensions;
using WoW.Two.Sdk.Beta.Be.Files.Module.Callers;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace Files.Infrastructure.Files.QueryHandlers;

/// <inheritdoc />
public sealed class GetAllFilesQueryHandler(IFileStoreBroker store, FilesCallerOverride callers)
    : IQueryHandler<GetAllFilesQuery, AppResult<FilePage>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<FilePage>> HandleAsync(GetAllFilesQuery request, CancellationToken ct)
    {
        using var caller = callers.Use(request.Actor);
        var result = await store.ListAsync(request.Criteria, ct);

        return result.Match(value => AppResult<FilePage>.Ok(value), error => AppResult<FilePage>.Fail(error));
    }
}
