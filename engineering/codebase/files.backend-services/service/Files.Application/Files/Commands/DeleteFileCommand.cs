using Files.Application.Access.Models;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace Files.Application.Files.Commands;

/// <summary>Soft-deletes a file.</summary>
public sealed record DeleteFileCommand(FileRequestActor Actor, FileId Id)
    : ICommand<AppResult<FileModel>>;
