using Files.Application.Status.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace Files.Application.Status.Queries;

/// <summary>Reads the build version and availability of image processing.</summary>
public sealed record GetFilesSystemStatusQuery : IQuery<AppResult<FilesSystemStatus>>;
