using WoW.Two.Sdk.Beta.Be.Files.Module.Imports.Commands;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Imports.CommandHandlers;

/// <inheritdoc />
internal sealed class PushFileImportEntryCommandHandler(IFilesImportsService imports)
    : ICommandHandler<PushFileImportEntryCommand, AppResult<FileImportPushResultModel>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<FileImportPushResultModel>> HandleAsync(PushFileImportEntryCommand request, CancellationToken ct)
    {
        var result = await imports.PushAsync(request.ImportId, request.Contents, request.Spec, ct);

        return result.Match(value => AppResult<FileImportPushResultModel>.Ok(value), error => AppResult<FileImportPushResultModel>.Fail(error));
    }
}
