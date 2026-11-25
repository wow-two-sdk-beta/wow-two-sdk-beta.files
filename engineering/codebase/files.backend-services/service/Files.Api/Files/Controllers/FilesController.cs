using Files.Api.Auth;
using Files.Application.Files.Commands;
using Files.Application.Files.Models;
using Files.Application.Files.Queries;
using WoW.Two.Sdk.Beta.Be.Files.Module.Callers;
using WoW.Two.Sdk.Beta.Be.Mediator;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;
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
public sealed class FilesController(ISender sender, IFilesCallerService caller) : ControllerBase
{
    /// <summary>Uploads a file: a multipart body whose fields (<c>name</c>, <c>appId</c>, <c>contentType</c>,
    /// <c>visibility</c>, <c>expiresAtUtc</c>, <c>variantPresets</c>) precede the streamed file part.</summary>
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

            var result = await sender.SendAsync(new UploadFileCommand(caller.ToActor(), section.Body, ((Result<UploadSpec>.Success)spec).Value), ct);
            return result.Match<IActionResult>(
                success => Created($"/{FilesRouteConstants.Files}/{success.Data.Id}", ApiResponse<FileModel>.Ok(success.Data)),
                failure => this.ToProblem(failure.Error));
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
        return Answer(await sender.SendAsync(new GetAllFilesQuery(caller.ToActor(), query), ct));
    }

    /// <summary>Gets a file's metadata.</summary>
    [HttpGet("{id}")]
    [Authorize(Policy = FilesScopeConstants.Read)]
    [ProducesResponseType<ApiResponse<FileModel>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(string id, CancellationToken ct) =>
        FileId.TryParse(id, out var fileId) ? Answer(await sender.SendAsync(new GetFileByIdQuery(caller.ToActor(), fileId), ct)) : NotFoundProblem();

    /// <summary>Streams the original, optionally with a byte range, or an image size requested by preset.</summary>
    [HttpGet("{id}/content")]
    [Authorize(Policy = FilesScopeConstants.Read)]
    public async Task<IActionResult> Content(string id, [FromQuery] string? preset, CancellationToken ct)
    {
        if (!FileId.TryParse(id, out var fileId))
            return NotFoundProblem();
        var query = new OpenFileContentQuery(caller.ToActor(), fileId, preset, Request.Headers.Range,
            Request.Headers.ContainsKey("Range"));
        var result = await sender.SendAsync(query, ct);
        if (result is AppResult<FileDownload>.Failure failure)
        {
            if (failure.Error.Metadata?.GetValueOrDefault(FileDownload.UnsatisfiedRangeLength) is long size)
                Response.Headers.ContentRange = $"bytes */{size}";

            return this.ToProblem(failure.Error);
        }

        var (file, content) = ((AppResult<FileDownload>.Success)result).Data;
        await using var bytes = content.Content;
        Response.StatusCode = content.Range is null ? StatusCodes.Status200OK : StatusCodes.Status206PartialContent;
        if (content.Range is { } served)
            Response.Headers.ContentRange = $"bytes {served.From}-{served.To}/{content.TotalLength}";
        Response.ContentType = content.ContentType;
        Response.ContentLength = content.Length;
        Response.Headers.XContentTypeOptions = "nosniff";
        var name = content.ContentType == "image/webp" && file.ContentType != "image/webp"
            ? Path.ChangeExtension(file.Name, ".webp")
            : file.Name;
        Response.Headers.ContentDisposition = FileNameMapper.ToContentDisposition(name, FileDisposition.Attachment);
        await bytes.CopyToAsync(Response.Body, ct);
        return new EmptyResult();
    }

    /// <summary>Changes a file's name, visibility or expiry.</summary>
    [HttpPatch("{id}")]
    [Authorize(Policy = FilesScopeConstants.Write)]
    [ProducesResponseType<ApiResponse<FileModel>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(string id, [FromBody] FilePatch patch, CancellationToken ct) =>
        FileId.TryParse(id, out var fileId) ? Answer(await sender.SendAsync(new UpdateFileCommand(caller.ToActor(), fileId, patch), ct)) : NotFoundProblem();

    /// <summary>Deletes a file softly.</summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = FilesScopeConstants.Write)]
    [ProducesResponseType<ApiResponse<FileModel>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct) =>
        FileId.TryParse(id, out var fileId) ? Answer(await sender.SendAsync(new DeleteFileCommand(caller.ToActor(), fileId), ct)) : NotFoundProblem();

    /// <summary>Restores a deleted file within its retention.</summary>
    [HttpPost("{id}/restore")]
    [Authorize(Policy = FilesScopeConstants.Write)]
    [ProducesResponseType<ApiResponse<FileModel>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Restore(string id, CancellationToken ct) =>
        FileId.TryParse(id, out var fileId) ? Answer(await sender.SendAsync(new RestoreFileCommand(caller.ToActor(), fileId), ct)) : NotFoundProblem();

    /// <summary>Lists the records that use a file.</summary>
    [HttpGet("{id}/references")]
    [Authorize(Policy = FilesScopeConstants.Read)]
    [ProducesResponseType<ApiResponse<IReadOnlyList<FileReference>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> References(string id, CancellationToken ct) =>
        FileId.TryParse(id, out var fileId) ? Answer(await sender.SendAsync(new GetFileReferencesQuery(caller.ToActor(), fileId), ct)) : NotFoundProblem();

    /// <summary>Attaches a file to a record; attaching twice is a no-op.</summary>
    [HttpPut("{id}/references/{recordType}/{recordId}")]
    [Authorize(Policy = FilesScopeConstants.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Attach(string id, string recordType, string recordId, CancellationToken ct) =>
        FileId.TryParse(id, out var fileId) ? Answer(await sender.SendAsync(new AttachFileReferenceCommand(caller.ToActor(), fileId, new FileReference(recordType, recordId)), ct)) : NotFoundProblem();

    /// <summary>Detaches a file from a record.</summary>
    [HttpDelete("{id}/references/{recordType}/{recordId}")]
    [Authorize(Policy = FilesScopeConstants.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Detach(string id, string recordType, string recordId, CancellationToken ct) =>
        FileId.TryParse(id, out var fileId) ? Answer(await sender.SendAsync(new DetachFileReferenceCommand(caller.ToActor(), fileId, new FileReference(recordType, recordId)), ct)) : NotFoundProblem();

    /// <summary>Issues a short-lived signed URL on the content host, to the original or the variant a width picks.</summary>
    [HttpPost("{id}/urls")]
    [Authorize(Policy = FilesScopeConstants.Read)]
    [ProducesResponseType<ApiResponse<SignedFileUrl>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> IssueUrl(string id, [FromBody] UrlSpec spec, CancellationToken ct) =>
        FileId.TryParse(id, out var fileId) ? Answer(await sender.SendAsync(new IssueFileUrlCommand(caller.ToActor(), fileId, spec), ct)) : NotFoundProblem();

    /// <summary>Gets the immutable URL of a public file, or of the variant a <c>width</c> picks.</summary>
    [HttpGet("{id}/public-url")]
    [Authorize(Policy = FilesScopeConstants.Read)]
    [ProducesResponseType<ApiResponse<Uri>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> PublicUrl(string id, [FromQuery] int? width, CancellationToken ct) =>
        FileId.TryParse(id, out var fileId) ? Answer(await sender.SendAsync(new GetPublicFileUrlQuery(caller.ToActor(), fileId, width), ct)) : NotFoundProblem();

    private IActionResult Answer<T>(AppResult<T> result) where T : notnull =>
        result.Match<IActionResult>(success => Ok(ApiResponse<T>.Ok(success.Data)), failure => this.ToProblem(failure.Error));

    private IActionResult Answer(AppResult<Unit> result) =>
        result.Match<IActionResult>(_ => NoContent(), failure => this.ToProblem(failure.Error));

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
            VariantPresets = fields.GetValueOrDefault("variantPresets") switch
            {
                null => null,
                "" => [],
                var presets => presets.Split(',', 6, StringSplitOptions.TrimEntries),
            },
        });
    }
}
