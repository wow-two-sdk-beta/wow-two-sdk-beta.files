using Files.Application.Access.Models;
using WoW.Two.Sdk.Beta.Be.Files.Audit;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace Files.Application.Audit.Queries;

/// <summary>Pages audit entries visible to the authorized tenant.</summary>
public sealed record GetAllFileAuditEntriesQuery(FileRequestActor Actor, FileAuditQuery Criteria)
    : IQuery<AppResult<FileAuditPage>>;
