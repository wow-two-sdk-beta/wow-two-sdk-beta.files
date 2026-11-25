using Files.Application.Access.Models;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Files.Urls;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace Files.Application.Files.Queries;

/// <summary>Reads the immutable URL of a public file.</summary>
public sealed record GetPublicFileUrlQuery(FileRequestActor Actor, FileId Id, int? Width)
    : IQuery<AppResult<Uri>>;
