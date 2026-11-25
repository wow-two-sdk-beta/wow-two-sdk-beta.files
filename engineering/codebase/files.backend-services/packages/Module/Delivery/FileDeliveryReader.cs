using WoW.Two.Sdk.Beta.Be.Files.Module.Delivery.Models;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Delivery;

/// <summary>Applies content preconditions and opens the authorized representation for the HTTP adapter.</summary>
internal sealed class FileDeliveryReader(IFilesDeliveryService delivery)
{
    internal async Task<AppResult<FileDeliveryRead>> OpenAsync(Result<DeliverableFileModel> found, FileDeliveryConditions conditions, CancellationToken ct)
    {
        if (found is Result<DeliverableFileModel>.Failure failure)
            return AppResult<FileDeliveryRead>.Fail(failure.Error);

        var file = ((Result<DeliverableFileModel>.Success)found).Value;
        var etag = $"\"{file.Sha256}\"";
        if (conditions.IfNoneMatch.Any(value => value is not null && (value.Contains(etag, StringComparison.Ordinal) || value.Trim() == "*")))
            return AppResult<FileDeliveryRead>.Ok(new FileDeliveryRead(file, null, NotModified: true));

        if (!ByteRangeHeaderParser.TryParse(conditions.Range, file.Size, out var range))
            return AppResult<FileDeliveryRead>.Ok(new FileDeliveryRead(file, null, NotModified: false));

        if (await delivery.OpenAsync(file, range, ct) is not Result<FileContent>.Success { Value: var content })
            return AppResult<FileDeliveryRead>.Fail(AppErrorFactory.NotFound());

        return AppResult<FileDeliveryRead>.Ok(new FileDeliveryRead(file, content, NotModified: false));
    }
}
