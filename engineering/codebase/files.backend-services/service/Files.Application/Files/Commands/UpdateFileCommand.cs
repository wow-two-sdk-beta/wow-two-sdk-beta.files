using Files.Application.Access.Models;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace Files.Application.Files.Commands;

/// <summary>Updates a file's metadata.</summary>
public sealed record UpdateFileCommand(FileRequestActor Actor, FileId Id, FilePatch Patch)
    : ICommand<AppResult<FileModel>>;
