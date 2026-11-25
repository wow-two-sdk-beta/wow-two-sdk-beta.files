using Files.Application.Files.Commands;
using Files.Infrastructure.Access.Extensions;
using WoW.Two.Sdk.Beta.Be.Files.Module.Callers;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Files.Urls;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace Files.Infrastructure.Files.CommandHandlers;

/// <inheritdoc />
public sealed class IssueFileUrlCommandHandler(IFileUrlsBroker urls, FilesCallerOverride callers)
    : ICommandHandler<IssueFileUrlCommand, AppResult<SignedFileUrl>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<SignedFileUrl>> HandleAsync(IssueFileUrlCommand request, CancellationToken ct)
    {
        using var caller = callers.Use(request.Actor);
        var result = await urls.IssueAsync(request.Id, request.Spec, ct);

        return result.Match(value => AppResult<SignedFileUrl>.Ok(value), error => AppResult<SignedFileUrl>.Fail(error));
    }
}
