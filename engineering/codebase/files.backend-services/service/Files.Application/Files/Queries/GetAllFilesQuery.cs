using Files.Application.Access.Models;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace Files.Application.Files.Queries;

/// <summary>Pages the authorized tenant's files.</summary>
public sealed record GetAllFilesQuery(FileRequestActor Actor, FileQuery Criteria)
    : IQuery<AppResult<FilePage>>;
