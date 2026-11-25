using WoW.Two.Sdk.Beta.Be.Files.Module.Delivery.Models;
using WoW.Two.Sdk.Beta.Be.Files.Module.Delivery.Queries;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Delivery.QueryHandlers;

internal sealed class ReadPublicFileDeliveryQueryHandler(IFilesDeliveryService delivery, FileDeliveryReader reader)
    : IQueryHandler<ReadPublicFileDeliveryQuery, AppResult<FileDeliveryRead>>
{
    public async ValueTask<AppResult<FileDeliveryRead>> HandleAsync(ReadPublicFileDeliveryQuery query, CancellationToken ct)
    {
        var found = await delivery.FindPublicAsync(query.Tenant, query.Sha256, ct);

        return await reader.OpenAsync(found, query.Conditions, ct);
    }
}
