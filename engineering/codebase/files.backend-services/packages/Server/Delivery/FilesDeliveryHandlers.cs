using System.Globalization;
using Microsoft.AspNetCore.Http;
using WoW.Two.Sdk.Beta.Be.Files.Formats.Types;
using WoW.Two.Sdk.Beta.Be.Files.Module.Delivery;
using WoW.Two.Sdk.Beta.Be.Files.Module.Delivery.Models;
using WoW.Two.Sdk.Beta.Be.Files.Module.Delivery.Queries;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Files.Urls;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;
using WoW.Two.Sdk.Beta.Be.Mediator;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace WoW.Two.Sdk.Beta.Be.Files.Server.Delivery;

/// <summary>Serves files on the content host: private ones through signed URLs, public ones at immutable URLs; one
/// byte range, entity tags, and the user-content headers on every answer.</summary>
internal static class FilesDeliveryHandlers
{
    private const string PublicCaching = "public, max-age=31536000, immutable";

    /// <summary>Serves <c>/f/{id}</c> and <c>/t/{tenant}/f/{id}</c>; a <c>w</c> parameter names a variant.</summary>
    public static async Task ServeSignedAsync(HttpContext http, string id, string? tenant, ISender sender, TimeProvider time)
    {
        var query = http.Request.Query;
        int? width = null;
        if (!FileId.TryParse(id, out var fileId)
            || !long.TryParse(query["exp"], NumberStyles.None, CultureInfo.InvariantCulture, out var expires)
            || (query.ContainsKey("w") && !TryParseWidth(query["w"], out width)))
        {
            Refuse(http.Response, StatusCodes.Status403Forbidden);
            return;
        }

        var read = new ReadSignedFileDeliveryQuery(tenant, fileId, expires, query["d"], query["sig"], width, Conditions(http.Request));
        var result = await sender.SendAsync(read, http.RequestAborted);

        await ServeAsync(http, result, file =>
            $"private, max-age={Math.Max(0, (long)(file.ExpiresAtUtc!.Value - time.GetUtcNow()).TotalSeconds)}");
    }

    /// <summary>Serves <c>/m/{sha256}.{ext}</c> and <c>/t/{tenant}/m/{sha256}.{ext}</c>.</summary>
    public static async Task ServePublicAsync(HttpContext http, string file, string? tenant, ISender sender)
    {
        var sha256 = file.Split('.')[0];
        if (sha256.Length != FilesConstants.Sha256HexLength || !sha256.All(char.IsAsciiHexDigitLower))
        {
            Refuse(http.Response, StatusCodes.Status404NotFound);
            return;
        }

        var result = await sender.SendAsync(new ReadPublicFileDeliveryQuery(tenant, sha256, Conditions(http.Request)), http.RequestAborted);

        await ServeAsync(http, result, _ => PublicCaching);
    }

    private static FileDeliveryConditions Conditions(HttpRequest request) =>
        new(request.Headers.IfNoneMatch.ToArray(), request.Headers.Range);

    private static async Task ServeAsync(HttpContext http, AppResult<FileDeliveryRead> result, Func<DeliverableFileModel, string> caching)
    {
        var response = http.Response;
        if (result is AppResult<FileDeliveryRead>.Failure failure)
        {
            if (failure.Error.Type == AppErrorType.Unexpected)
                throw failure.Error.ToException();

            Refuse(response, failure.Error.Type == AppErrorType.Forbidden ? StatusCodes.Status403Forbidden : StatusCodes.Status404NotFound);
            return;
        }

        var read = ((AppResult<FileDeliveryRead>.Success)result).Data;
        var file = read.File;
        FilesDeliveryHeaders.Apply(response);
        response.Headers.ETag = $"\"{file.Sha256}\"";
        response.Headers.AcceptRanges = "bytes";
        response.Headers.CacheControl = caching(file);
        if (read.NotModified)
        {
            response.StatusCode = StatusCodes.Status304NotModified;
            return;
        }

        if (read.HasUnsatisfiedRange)
        {
            response.StatusCode = StatusCodes.Status416RangeNotSatisfiable;
            response.Headers.ContentRange = $"bytes */{file.Size}";
            return;
        }

        var content = read.Content!;
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

    /// <summary>Reads a signed URL's variant width: digits only, at least 1.</summary>
    private static bool TryParseWidth(string? value, out int? width)
    {
        width = int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) && parsed > 0 ? parsed : null;
        return width is not null;
    }

    private static void Refuse(HttpResponse response, int status)
    {
        FilesDeliveryHeaders.Apply(response);
        response.Headers.CacheControl = "no-store";
        response.StatusCode = status;
    }
}
