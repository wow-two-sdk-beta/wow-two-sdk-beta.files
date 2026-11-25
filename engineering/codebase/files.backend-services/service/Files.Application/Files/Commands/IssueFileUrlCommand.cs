using Files.Application.Access.Models;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Files.Urls;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace Files.Application.Files.Commands;

/// <summary>Issues a signed URL for file content.</summary>
public sealed record IssueFileUrlCommand(FileRequestActor Actor, FileId Id, UrlSpec Spec)
    : ICommand<AppResult<SignedFileUrl>>;
