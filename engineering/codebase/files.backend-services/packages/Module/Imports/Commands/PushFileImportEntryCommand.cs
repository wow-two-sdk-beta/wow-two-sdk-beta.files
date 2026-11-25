using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Imports.Commands;

/// <summary>Stores one keyed file in an import.</summary>
internal sealed record PushFileImportEntryCommand(string ImportId, Stream Contents, FileImportPushSpec Spec) : ICommand<AppResult<FileImportPushResultModel>>;
