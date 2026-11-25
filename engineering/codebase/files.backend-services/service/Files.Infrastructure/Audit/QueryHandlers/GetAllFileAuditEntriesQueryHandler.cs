using Files.Application.Audit.Queries;
using Files.Infrastructure.Access.Extensions;
using WoW.Two.Sdk.Beta.Be.Files.Audit;
using WoW.Two.Sdk.Beta.Be.Files.Module.Callers;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace Files.Infrastructure.Audit.QueryHandlers;

/// <inheritdoc />
public sealed class GetAllFileAuditEntriesQueryHandler(IFileAuditBroker audit, FilesCallerOverride callers)
    : IQueryHandler<GetAllFileAuditEntriesQuery, AppResult<FileAuditPage>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<FileAuditPage>> HandleAsync(GetAllFileAuditEntriesQuery request, CancellationToken ct)
    {
        using var caller = callers.Use(request.Actor);
        var result = await audit.ListAsync(request.Criteria, ct);

        return result.Match(value => AppResult<FileAuditPage>.Ok(value), error => AppResult<FileAuditPage>.Fail(error));
    }
}
