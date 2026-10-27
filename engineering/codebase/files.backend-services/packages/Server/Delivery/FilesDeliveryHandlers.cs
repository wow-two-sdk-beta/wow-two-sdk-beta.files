using System.Globalization;
using Microsoft.AspNetCore.Http;
using WoW.Two.Sdk.Beta.Be.Files.Formats.Types;
using WoW.Two.Sdk.Beta.Be.Files.Module.Delivery;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Files.Urls;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;

namespace WoW.Two.Sdk.Beta.Be.Files.Server.Delivery;

/// <summary>Serves files on the content host: private ones through signed URLs, public ones at immutable URLs; one
/// byte range, entity tags, and the user-content headers on every answer.</summary>
internal static class FilesDeliveryHandlers
{
    private const string PublicCaching = "public, max-age=31536000, immutable";

    /// <summary>Serves <c>/f/{id}</c> and <c>/t/{tenant}/f/{id}</c>.</summary>
    public static async Task ServeSignedAsync(HttpContext http, string id, string? tenant, IFilesDeliveryService delivery, TimeProvider time)
    {
        var query = http.Request.Query;
        if (!FileId.TryParse(id, out var fileId)
            || !long.TryParse(query["exp"], NumberStyles.None, CultureInfo.InvariantCulture, out var expires))
        {
            Refuse(http.Response, StatusCodes.Status403Forbidden);
            return;
        }

        var found = await delivery.FindSignedAsync(tenant, fileId, expires, query["d"], query["sig"], http.RequestAborted);
        await ServeAsync(http, delivery, found, file =>
            $"private, max-age={Math.Max(0, (long)(file.ExpiresAtUtc!.Value - time.GetUtcNow()).TotalSeconds)}");
    }

    /// <summary>Serves <c>/m/{sha256}.{ext}</c> and <c>/t/{tenant}/m/{sha256}.{ext}</c>.</summary>
    public static async Task ServePublicAsync(HttpContext http, string file, string? tenant, IFilesDeliveryService delivery)
    {
        var sha256 = file.Split('.')[0];
        if (sha256.Length != FilesConstants.Sha256HexLength || !sha256.All(char.IsAsciiHexDigitLower))
        {
            Refuse(http.Response, StatusCodes.Status404NotFound);
            return;
        }

        var found = await delivery.FindPublicAsync(tenant, sha256, http.RequestAborted);
        await ServeAsync(http, delivery, found, _ => PublicCaching);
    }

    private static async Task ServeAsync(HttpContext http, IFilesDeliveryService delivery, Result<DeliverableFileModel> found, Func<DeliverableFileModel, string> caching)
    {
        var response = http.Response;
        if (found is not Result<DeliverableFileModel>.Success { Value: var file })
        {
            var error = ((Result<DeliverableFileModel>.Failure)found).Error;
            Refuse(response, error.Type == AppErrorType.Forbidden ? StatusCodes.Status403Forbidden : StatusCodes.Status404NotFound);
            return;
        }

        var etag = $"\"{file.Sha256}\"";
        FilesDeliveryHeaders.Apply(response);
        response.Headers.ETag = etag;
        response.Headers.AcceptRanges = "bytes";
        response.Headers.CacheControl = caching(file);
        if (http.Request.Headers.IfNoneMatch.Any(value => value is not null && (value.Contains(etag, StringComparison.Ordinal) || value.Trim() == "*")))
        {
            response.StatusCode = StatusCodes.Status304NotModified;
            return;
        }

        if (!ByteRangeHeaderParser.TryParse(http.Request.Headers.Range, file.Size, out var range))
        {
            response.StatusCode = StatusCodes.Status416RangeNotSatisfiable;
            response.Headers.ContentRange = $"bytes */{file.Size}";
            return;
        }

        if (await delivery.OpenAsync(file, range, http.RequestAborted) is not Result<FileContent>.Success { Value: var content })
        {
            Refuse(response, StatusCodes.Status404NotFound);
            return;
        }

        await using var bytes = content.Content;
        var disposition = file.Risk == FileRisk.None ? file.Disposition : FileDisposition.Attachment;
        response.StatusCode = content.Range is null ? StatusCodes.Status200OK : StatusCodes.Status206PartialContent;
        if (content.Range is { } served)
            response.Headers.ContentRange = $"bytes {served.From}-{served.To}/{content.TotalLength}";
        response.ContentType = file.ContentType;
        response.ContentLength = content.Length;
        response.Headers.ContentDisposition = FileNameMapper.ToContentDisposition(file.Name, disposition);
        if (!HttpMethods.IsHead(http.Request.Method))
            await bytes.CopyToAsync(response.Body, http.RequestAborted);
    }

    private static void Refuse(HttpResponse response, int status)
    {
        FilesDeliveryHeaders.Apply(response);
        response.Headers.CacheControl = "no-store";
        response.StatusCode = status;
    }
}
