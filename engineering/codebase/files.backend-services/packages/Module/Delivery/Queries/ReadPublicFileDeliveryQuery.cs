using WoW.Two.Sdk.Beta.Be.Files.Module.Delivery.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Delivery.Queries;

/// <summary>Finds and opens a public content download.</summary>
internal sealed record ReadPublicFileDeliveryQuery(string? Tenant, string Sha256, FileDeliveryConditions Conditions)
    : IQuery<AppResult<FileDeliveryRead>>;
