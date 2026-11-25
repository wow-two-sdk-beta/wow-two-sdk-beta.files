using Files.Application.Files.Queries;
using Files.Infrastructure.Access.Extensions;
using WoW.Two.Sdk.Beta.Be.Files.Module.Callers;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Files.Urls;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace Files.Infrastructure.Files.QueryHandlers;

/// <inheritdoc />
public sealed class GetPublicFileUrlQueryHandler(IFileUrlsBroker urls, FilesCallerOverride callers)
    : IQueryHandler<GetPublicFileUrlQuery, AppResult<Uri>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<Uri>> HandleAsync(GetPublicFileUrlQuery request, CancellationToken ct)
    {
        using var caller = callers.Use(request.Actor);
        var result = await urls.GetPublicUrlAsync(request.Id, request.Width, ct);

        return result.Match(value => AppResult<Uri>.Ok(value), error => AppResult<Uri>.Fail(error));
    }
}
