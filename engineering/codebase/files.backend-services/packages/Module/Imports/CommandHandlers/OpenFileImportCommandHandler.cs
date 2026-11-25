using WoW.Two.Sdk.Beta.Be.Files.Module.Imports.Commands;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Imports.CommandHandlers;

/// <inheritdoc />
internal sealed class OpenFileImportCommandHandler(IFilesImportsService imports)
    : ICommandHandler<OpenFileImportCommand, AppResult<FileImportTicketModel>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<FileImportTicketModel>> HandleAsync(OpenFileImportCommand request, CancellationToken ct)
    {
        var result = await imports.OpenAsync(request.Spec, ct);

        return result.Match(value => AppResult<FileImportTicketModel>.Ok(value), error => AppResult<FileImportTicketModel>.Fail(error));
    }
}
