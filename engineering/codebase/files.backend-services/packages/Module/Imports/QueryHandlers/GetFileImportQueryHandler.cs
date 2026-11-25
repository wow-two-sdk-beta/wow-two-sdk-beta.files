using WoW.Two.Sdk.Beta.Be.Files.Module.Imports.Queries;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Imports.QueryHandlers;

/// <inheritdoc />
internal sealed class GetFileImportQueryHandler(IFilesImportsService imports)
    : IQueryHandler<GetFileImportQuery, AppResult<FileImportModel>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<FileImportModel>> HandleAsync(GetFileImportQuery request, CancellationToken ct)
    {
        var result = await imports.GetAsync(request.ImportId, ct);

        return result.Match(value => AppResult<FileImportModel>.Ok(value), error => AppResult<FileImportModel>.Fail(error));
    }
}
