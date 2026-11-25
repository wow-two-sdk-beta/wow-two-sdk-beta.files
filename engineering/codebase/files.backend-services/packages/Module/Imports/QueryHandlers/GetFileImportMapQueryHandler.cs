using WoW.Two.Sdk.Beta.Be.Files.Module.Imports.Queries;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Imports.QueryHandlers;

/// <inheritdoc />
internal sealed class GetFileImportMapQueryHandler(IFilesImportsService imports)
    : IQueryHandler<GetFileImportMapQuery, AppResult<FileImportMapPage>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<FileImportMapPage>> HandleAsync(GetFileImportMapQuery request, CancellationToken ct)
    {
        var result = await imports.MapAsync(request.ImportId, request.Cursor, request.Limit, ct);

        return result.Match(value => AppResult<FileImportMapPage>.Ok(value), error => AppResult<FileImportMapPage>.Fail(error));
    }
}
