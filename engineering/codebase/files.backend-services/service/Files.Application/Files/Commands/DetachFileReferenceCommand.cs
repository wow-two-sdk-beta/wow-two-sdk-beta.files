using Files.Application.Access.Models;
using WoW.Two.Sdk.Beta.Be.Files.References;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Mediator;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace Files.Application.Files.Commands;

/// <summary>Detaches a file from a record.</summary>
public sealed record DetachFileReferenceCommand(FileRequestActor Actor, FileId Id, FileReference Reference)
    : ICommand<AppResult<Unit>>;
