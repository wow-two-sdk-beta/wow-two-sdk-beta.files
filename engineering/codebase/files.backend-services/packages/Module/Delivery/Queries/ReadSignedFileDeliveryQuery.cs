using WoW.Two.Sdk.Beta.Be.Files.Module.Delivery.Models;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Delivery.Queries;

/// <summary>Finds and opens a signed content download.</summary>
internal sealed record ReadSignedFileDeliveryQuery(string? Tenant, FileId Id, long Expires, string? Disposition, string? Signature, int? Width, FileDeliveryConditions Conditions)
    : IQuery<AppResult<FileDeliveryRead>>;
