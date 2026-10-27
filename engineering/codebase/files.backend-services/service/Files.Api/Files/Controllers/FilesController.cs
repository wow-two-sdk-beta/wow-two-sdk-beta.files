using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using Files.Api.Files.Filters;
using Files.Api.Files.Mappers;
using WoW.Two.Sdk.Beta.Be.Files;
using WoW.Two.Sdk.Beta.Be.Files.Formats.Types;
using WoW.Two.Sdk.Beta.Be.Files.References;
using WoW.Two.Sdk.Beta.Be.Files.Server.Delivery;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Files.Uploads;
using WoW.Two.Sdk.Beta.Be.Files.Urls;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;
using WoW.Two.Sdk.Beta.Be.Web.Contracts;
using WoW.Two.Sdk.Beta.Be.Web.ExceptionHandling.Extensions;

namespace Files.Api.Files.Controllers;

/// <summary>Exposes the Files brokers over HTTP — the mirror the Client package calls. Reads take <c>files.read</c>,
/// changes <c>files.write</c>; each action names its own scope, so one scope never needs the other.</summary>
[ApiController]
[Route(FilesRouteConstants.Files)]
[Authorize]
public sealed class FilesController(
    IFileUploadsBroker uploads,
    IFileStoreBroker store,
    IFileReferencesBroker references,
    IFileUrlsBroker urls) : ControllerBase
{
    /// <summary>Uploads a file: a multipart body whose fields (<c>name</c>, <c>appId</c>, <c>contentType</c>,
    /// <c>visibility</c>, <c>expiresAtUtc</c>) come before the file part, which is read as a stream.</summary>
    /// <param name="idempotencyKey">The key that makes a retry return the first upload's file.</param>
    /// <param name="ct">The token that cancels the request.</param>
    [HttpPost]
    [Authorize(Policy = FilesScopeConstants.Write)]
    [DisableRequestSizeLimit]
    [DisableFormValueBinding]
    [ProducesResponseType<ApiResponse<FileModel>>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload([FromHeader(Name = FilesRouteConstants.IdempotencyKeyHeader)] string? idempotencyKey, CancellationToken ct)
    {
        if (!MediaTypeHeaderValue.TryParse(Request.ContentType, out var mediaType) || HeaderUtilities.RemoveQuotes(mediaType.Boundary).Value is not { Length: > 0 } boundary)
            return this.ToProblem(AppErrorFactory.Validation("The upload must be multipart/form-data."));

        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var reader = new MultipartReader(boundary, Request.Body);
        while (await reader.ReadNextSectionAsync(ct) is { } section)
        {
            if (!ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var disposition))
                continue;

            var fieldName = HeaderUtilities.RemoveQuotes(disposition.Name).Value ?? string.Empty;
            if (!disposition.IsFileDisposition())
            {
                using var text = new StreamReader(section.Body, leaveOpen: true);
                fields[fieldName] = await text.ReadToEndAsync(ct);
                continue;
            }

            var spec = ToSpec(fields, HeaderUtilities.RemoveQuotes(disposition.FileNameStar).Value ?? HeaderUtilities.RemoveQuotes(disposition.FileName).Value, section.ContentType, idempotencyKey);
            if (spec is Result<UploadSpec>.Failure invalid)
                return this.ToProblem(invalid.Error);

            var result = await uploads.UploadAsync(section.Body, ((Result<UploadSpec>.Success)spec).Value, ct);
            return result.Match<IActionResult>(
                file => Created($"/{FilesRouteConstants.Files}/{file.Id}", ApiResponse<FileModel>.Ok(file)),
                error => this.ToProblem(error));
        }

        return this.ToProblem(AppErrorFactory.Validation("The upload has no file part."));
    }

    /// <summary>Lists the tenant's files, newest first.</summary>
    [HttpGet]
    [Authorize(Policy = FilesScopeConstants.Read)]
    [ProducesResponseType<ApiResponse<FilePage>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] string? app, [FromQuery] string? owner, [FromQuery] string? kind, [FromQuery] string? state,
        [FromQuery] DateTimeOffset? createdAfter, [FromQuery] bool includeDeleted, [FromQuery] string? cursor,
        [FromQuery] int? limit, CancellationToken ct)
    {
        if (!WireEnumMapper.TryParse<FileKind>(kind, out var fileKind) || !WireEnumMapper.TryParse<FileState>(state, out var fileState))
            return this.ToProblem(AppErrorFactory.Validation("The kind or state filter is not known."));

        var query = new FileQuery
        {
            AppId = app,
            OwnerId = owner,
            Kind = fileKind,
            State = fileState,
            CreatedAfterUtc = createdAfter,
            IncludeDeleted = includeDeleted,
            Cursor = cursor,
            Limit = limit ?? 50,
        };
        return Answer(await store.ListAsync(query, ct));
    }

    /// <summary>Gets a file's metadata.</summary>
    [HttpGet("{id}")]
    [Authorize(Policy = FilesScopeConstants.Read)]
    [ProducesResponseType<ApiResponse<FileModel>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(string id, CancellationToken ct) =>
        FileId.TryParse(id, out var fileId) ? Answer(await store.GetAsync(fileId, ct)) : NotFoundProblem();

    /// <summary>Streams a file's bytes; one <c>Range</c> answers <c>206</c>, one outside the file <c>416</c>.</summary>
    [HttpGet("{id}/content")]
    [Authorize(Policy = FilesScopeConstants.Read)]
    public async Task<IActionResult> Content(string id, CancellationToken ct)
    {
        if (!FileId.TryParse(id, out var fileId))
            return NotFoundProblem();
        if (await store.GetAsync(fileId, ct) is not Result<FileModel>.Success { Value: var file })
            return NotFoundProblem();
        if (!ByteRangeHeaderParser.TryParse(Request.Headers.Range, file.Size, out var range))
        {
            Response.Headers.ContentRange = $"bytes */{file.Size}";
            return this.ToProblem(FilesErrors.RangeInvalid(file.Size));
        }

        var opened = await store.OpenReadAsync(fileId, range, ct);
        if (opened is not Result<FileContent>.Success { Value: var content })
            return this.ToProblem(((Result<FileContent>.Failure)opened).Error);

        await using var bytes = content.Content;
        Response.StatusCode = content.Range is null ? StatusCodes.Status200OK : StatusCodes.Status206PartialContent;
        if (content.Range is { } served)
            Response.Headers.ContentRange = $"bytes {served.From}-{served.To}/{content.TotalLength}";
        Response.ContentType = content.ContentType;
        Response.ContentLength = content.Length;
        Response.Headers.XContentTypeOptions = "nosniff";
        Response.Headers.ContentDisposition = FileNameMapper.ToContentDisposition(file.Name, FileDisposition.Attachment);
        await bytes.CopyToAsync(Response.Body, ct);
        return new EmptyResult();
    }

    /// <summary>Changes a file's name, visibility or expiry.</summary>
    [HttpPatch("{id}")]
    [Authorize(Policy = FilesScopeConstants.Write)]
    [ProducesResponseType<ApiResponse<FileModel>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(string id, [FromBody] FilePatch patch, CancellationToken ct) =>
        FileId.TryParse(id, out var fileId) ? Answer(await store.UpdateAsync(fileId, patch, ct)) : NotFoundProblem();

    /// <summary>Deletes a file softly.</summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = FilesScopeConstants.Write)]
    [ProducesResponseType<ApiResponse<FileModel>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct) =>
        FileId.TryParse(id, out var fileId) ? Answer(await store.DeleteAsync(fileId, ct)) : NotFoundProblem();

    /// <summary>Restores a deleted file within its retention.</summary>
    [HttpPost("{id}/restore")]
    [Authorize(Policy = FilesScopeConstants.Write)]
    [ProducesResponseType<ApiResponse<FileModel>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Restore(string id, CancellationToken ct) =>
        FileId.TryParse(id, out var fileId) ? Answer(await store.RestoreAsync(fileId, ct)) : NotFoundProblem();

    /// <summary>Lists the records that use a file.</summary>
    [HttpGet("{id}/references")]
    [Authorize(Policy = FilesScopeConstants.Read)]
    [ProducesResponseType<ApiResponse<IReadOnlyList<FileReference>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> References(string id, CancellationToken ct) =>
        FileId.TryParse(id, out var fileId) ? Answer(await references.ListAsync(fileId, ct)) : NotFoundProblem();

    /// <summary>Attaches a file to a record; attaching twice is a no-op.</summary>
    [HttpPut("{id}/references/{recordType}/{recordId}")]
    [Authorize(Policy = FilesScopeConstants.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Attach(string id, string recordType, string recordId, CancellationToken ct) =>
        FileId.TryParse(id, out var fileId) ? Answer(await references.AttachAsync(fileId, new FileReference(recordType, recordId), ct)) : NotFoundProblem();

    /// <summary>Detaches a file from a record.</summary>
    [HttpDelete("{id}/references/{recordType}/{recordId}")]
    [Authorize(Policy = FilesScopeConstants.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Detach(string id, string recordType, string recordId, CancellationToken ct) =>
        FileId.TryParse(id, out var fileId) ? Answer(await references.DetachAsync(fileId, new FileReference(recordType, recordId), ct)) : NotFoundProblem();

    /// <summary>Issues a short-lived signed URL on the content host.</summary>
    [HttpPost("{id}/urls")]
    [Authorize(Policy = FilesScopeConstants.Read)]
    [ProducesResponseType<ApiResponse<SignedFileUrl>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> IssueUrl(string id, [FromBody] UrlSpec spec, CancellationToken ct) =>
        FileId.TryParse(id, out var fileId) ? Answer(await urls.IssueAsync(fileId, spec, ct)) : NotFoundProblem();

    /// <summary>Gets the immutable URL of a public file.</summary>
    [HttpGet("{id}/public-url")]
    [Authorize(Policy = FilesScopeConstants.Read)]
    [ProducesResponseType<ApiResponse<Uri>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> PublicUrl(string id, [FromQuery] string? variant, CancellationToken ct) =>
        FileId.TryParse(id, out var fileId) ? Answer(await urls.GetPublicUrlAsync(fileId, variant, ct)) : NotFoundProblem();

    private IActionResult Answer<T>(Result<T> result) where T : notnull =>
        result.Match<IActionResult>(value => Ok(ApiResponse<T>.Ok(value)), error => this.ToProblem(error));

    private IActionResult Answer(Result result) =>
        result.Match<IActionResult>(NoContent, error => this.ToProblem(error));

    private IActionResult NotFoundProblem() => this.ToProblem(FilesErrors.FileNotFound());

    /// <summary>Builds the upload's spec from the fields read before the file part.</summary>
    private static Result<UploadSpec> ToSpec(Dictionary<string, string> fields, string? fileName, string? partType, string? idempotencyKey)
    {
        if (!WireEnumMapper.TryParse<FileVisibility>(fields.GetValueOrDefault("visibility"), out var visibility))
            return Result<UploadSpec>.Fail(AppErrorFactory.Validation("The visibility is not known."));

        DateTimeOffset? expiresAt = null;
        if (fields.GetValueOrDefault("expiresAtUtc") is { Length: > 0 } expires)
        {
            if (!DateTimeOffset.TryParse(expires, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
                return Result<UploadSpec>.Fail(AppErrorFactory.Validation("The expiry is not a date."));
            expiresAt = parsed;
        }

        return Result<UploadSpec>.Ok(new UploadSpec
        {
            Name = fields.GetValueOrDefault("name") is { Length: > 0 } name ? name : fileName ?? string.Empty,
            ContentType = fields.GetValueOrDefault("contentType") ?? partType,
            AppId = fields.GetValueOrDefault("appId"),
            Visibility = visibility ?? FileVisibility.Private,
            IdempotencyKey = idempotencyKey,
            ExpiresAtUtc = expiresAt,
        });
    }
}
