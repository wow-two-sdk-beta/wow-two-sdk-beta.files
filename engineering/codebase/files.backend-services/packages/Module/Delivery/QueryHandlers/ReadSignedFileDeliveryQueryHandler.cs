using WoW.Two.Sdk.Beta.Be.Files.Module.Delivery.Models;
using WoW.Two.Sdk.Beta.Be.Files.Module.Delivery.Queries;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Delivery.QueryHandlers;

internal sealed class ReadSignedFileDeliveryQueryHandler(IFilesDeliveryService delivery, FileDeliveryReader reader)
    : IQueryHandler<ReadSignedFileDeliveryQuery, AppResult<FileDeliveryRead>>
{
    public async ValueTask<AppResult<FileDeliveryRead>> HandleAsync(ReadSignedFileDeliveryQuery query, CancellationToken ct)
    {
        var found = await delivery.FindSignedAsync(query.Tenant, query.Id, query.Expires, query.Disposition, query.Signature, query.Width, ct);

        return await reader.OpenAsync(found, query.Conditions, ct);
    }
}
