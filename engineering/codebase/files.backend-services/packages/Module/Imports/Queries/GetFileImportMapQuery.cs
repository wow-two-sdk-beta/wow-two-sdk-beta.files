using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Imports.Queries;

/// <summary>Reads one page of the file import's ID map.</summary>
internal sealed record GetFileImportMapQuery(string ImportId, string? Cursor, int Limit) : IQuery<AppResult<FileImportMapPage>>;
