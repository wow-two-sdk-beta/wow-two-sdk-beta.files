using Files.Application.Access.Models;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Files.Uploads;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace Files.Application.Files.Commands;

/// <summary>Stores a file from a multipart upload.</summary>
public sealed record UploadFileCommand(FileRequestActor Actor, Stream Contents, UploadSpec Spec)
    : ICommand<AppResult<FileModel>>;
