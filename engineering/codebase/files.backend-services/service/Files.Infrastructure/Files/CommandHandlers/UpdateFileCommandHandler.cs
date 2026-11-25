using Files.Application.Files.Commands;
using Files.Infrastructure.Access.Extensions;
using WoW.Two.Sdk.Beta.Be.Files.Module.Callers;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace Files.Infrastructure.Files.CommandHandlers;

/// <inheritdoc />
public sealed class UpdateFileCommandHandler(IFileStoreBroker store, FilesCallerOverride callers)
    : ICommandHandler<UpdateFileCommand, AppResult<FileModel>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<FileModel>> HandleAsync(UpdateFileCommand request, CancellationToken ct)
    {
        using var caller = callers.Use(request.Actor);
        var result = await store.UpdateAsync(request.Id, request.Patch, ct);

        return result.Match(value => AppResult<FileModel>.Ok(value), error => AppResult<FileModel>.Fail(error));
    }
}
