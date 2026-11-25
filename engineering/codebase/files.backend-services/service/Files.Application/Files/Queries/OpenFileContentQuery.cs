using Files.Application.Access.Models;
using Files.Application.Files.Models;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace Files.Application.Files.Queries;

/// <summary>Opens the original or a preset image after resolving the authorized file.</summary>
public sealed record OpenFileContentQuery(FileRequestActor Actor, FileId Id, string? Preset, string? Range, bool HasRange)
    : IQuery<AppResult<FileDownload>>;
