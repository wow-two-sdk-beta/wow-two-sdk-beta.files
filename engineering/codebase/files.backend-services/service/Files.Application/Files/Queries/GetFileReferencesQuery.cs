using Files.Application.Access.Models;
using WoW.Two.Sdk.Beta.Be.Files.References;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace Files.Application.Files.Queries;

/// <summary>Reads the records that reference a file.</summary>
public sealed record GetFileReferencesQuery(FileRequestActor Actor, FileId Id)
    : IQuery<AppResult<IReadOnlyList<FileReference>>>;
