using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Imports.Queries;

/// <summary>Reads the file import and its source counts.</summary>
internal sealed record GetFileImportQuery(string ImportId) : IQuery<AppResult<FileImportModel>>;
