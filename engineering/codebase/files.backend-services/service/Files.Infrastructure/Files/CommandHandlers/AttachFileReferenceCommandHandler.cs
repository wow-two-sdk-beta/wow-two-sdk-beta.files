using Files.Application.Files.Commands;
using Files.Infrastructure.Access.Extensions;
using WoW.Two.Sdk.Beta.Be.Files.Module.Callers;
using WoW.Two.Sdk.Beta.Be.Files.References;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;
using WoW.Two.Sdk.Beta.Be.Mediator;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace Files.Infrastructure.Files.CommandHandlers;

/// <inheritdoc />
public sealed class AttachFileReferenceCommandHandler(IFileReferencesBroker references, FilesCallerOverride callers)
    : ICommandHandler<AttachFileReferenceCommand, AppResult<Unit>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<Unit>> HandleAsync(AttachFileReferenceCommand request, CancellationToken ct)
    {
        using var caller = callers.Use(request.Actor);
        var result = await references.AttachAsync(request.Id, request.Reference, ct);

        return result.Match(() => AppResult<Unit>.Ok(Unit.Value), error => AppResult<Unit>.Fail(error));
    }
}
