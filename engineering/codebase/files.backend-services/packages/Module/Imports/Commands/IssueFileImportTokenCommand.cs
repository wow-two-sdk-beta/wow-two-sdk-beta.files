using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Imports.Commands;

/// <summary>Issues a token for a file import.</summary>
internal sealed record IssueFileImportTokenCommand(string ImportId) : ICommand<AppResult<FileImportTokenModel>>;
