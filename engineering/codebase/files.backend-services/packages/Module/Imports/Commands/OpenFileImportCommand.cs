using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Imports.Commands;

/// <summary>Opens a file import with an initial push token.</summary>
internal sealed record OpenFileImportCommand(FileImportSpec Spec) : ICommand<AppResult<FileImportTicketModel>>;
