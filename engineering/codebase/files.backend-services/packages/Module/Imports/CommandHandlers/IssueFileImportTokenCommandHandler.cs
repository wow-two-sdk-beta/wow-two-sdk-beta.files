using WoW.Two.Sdk.Beta.Be.Files.Module.Imports.Commands;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Imports.CommandHandlers;

/// <inheritdoc />
internal sealed class IssueFileImportTokenCommandHandler(IFilesImportsService imports)
    : ICommandHandler<IssueFileImportTokenCommand, AppResult<FileImportTokenModel>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<FileImportTokenModel>> HandleAsync(IssueFileImportTokenCommand request, CancellationToken ct)
    {
        var result = await imports.IssueTokenAsync(request.ImportId, ct);

        return result.Match(value => AppResult<FileImportTokenModel>.Ok(value), error => AppResult<FileImportTokenModel>.Fail(error));
    }
}
