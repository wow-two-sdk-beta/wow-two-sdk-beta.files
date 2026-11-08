using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using Files.Api.Auth;
using Files.Api.Files.Filters;
using Files.Api.Imports.Models;
using WoW.Two.Sdk.Beta.Be.Files;
using WoW.Two.Sdk.Beta.Be.Files.Module.Imports;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;
using WoW.Two.Sdk.Beta.Be.Web.Contracts;
using WoW.Two.Sdk.Beta.Be.Web.ExceptionHandling.Extensions;

namespace Files.Api.Imports.Controllers;

/// <summary>Exposes imports: the operator (Wheelhouse) opens one and mints its tokens; the mover pushes files with the
/// import's token (<c>X-Import-Token</c>) and reads the id map the product's cutover takes.</summary>
[ApiController]
[Route(FileImportConstants.Route)]
public sealed class ImportsController(IFilesImportsService imports) : ControllerBase
{
    /// <summary>Holds the page size of a map read that names none.</summary>
    private const int DefaultMapPageSize = 500;

    /// <summary>Opens an import for a tenant's sources and answers its first token.</summary>
    /// <param name="request">The tenant and the sources.</param>
    /// <param name="ct">The token that cancels the request.</param>
    [HttpPost]
    [Authorize(Policy = FilesScopeConstants.Tenants)]
    [ProducesResponseType<ApiResponse<FileImportTicketModel>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Open([FromBody] OpenImportRequest request, CancellationToken ct)
    {
        var opened = await imports.OpenAsync(new FileImportSpec { Tenant = request.Tenant ?? string.Empty, Sources = request.Sources ?? [] }, ct);
        return opened.Match<IActionResult>(
            ticket => Created($"/{FileImportConstants.Route}/{ticket.Id}", ApiResponse<FileImportTicketModel>.Ok(ticket)),
            error => this.ToProblem(error));
    }

    /// <summary>Mints a new token for an import.</summary>
    /// <param name="id">The import.</param>
    /// <param name="ct">The token that cancels the request.</param>
    [HttpPost("{id}/tokens")]
    [Authorize(Policy = FilesScopeConstants.Tenants)]
    [ProducesResponseType<ApiResponse<FileImportTokenModel>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> IssueToken(string id, CancellationToken ct) => Answer(await imports.IssueTokenAsync(id, ct));

    /// <summary>Gets an import and each source's counts.</summary>
    /// <param name="id">The import.</param>
    /// <param name="ct">The token that cancels the request.</param>
    [HttpGet("{id}")]
    [Authorize(Policy = FilesAuthConstants.ImportsPolicy)]
    [ProducesResponseType<ApiResponse<FileImportModel>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(string id, CancellationToken ct) => Answer(await imports.GetAsync(id, ct));

    /// <summary>Pages an import's id map in push order.</summary>
    /// <param name="id">The import.</param>
    /// <param name="cursor">The previous page's <c>next</c>.</param>
    /// <param name="limit">The page size, 1 to 1000; 500 when unset.</param>
    /// <param name="ct">The token that cancels the request.</param>
    [HttpGet("{id}/map")]
    [Authorize(Policy = FilesAuthConstants.ImportsPolicy)]
    [ProducesResponseType<ApiResponse<FileImportMapPage>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Map(string id, [FromQuery] string? cursor, [FromQuery] int? limit, CancellationToken ct) =>
        Answer(await imports.MapAsync(id, cursor, limit ?? DefaultMapPageSize, ct));

    /// <summary>Pushes one file: a multipart body whose fields (<c>source</c>, <c>key</c>, <c>sha256</c>, <c>reference</c>,
    /// <c>name</c>, <c>contentType</c>, <c>app</c>) come before the file part, which is read as a stream. Answers 201 when
    /// stored, 200 when the key is there with the same SHA-256, 409 with another, 422 when the bytes contradict
    /// <c>sha256</c>.</summary>
    /// <param name="id">The import.</param>
    /// <param name="ct">The token that cancels the request.</param>
    [HttpPost("{id}/files")]
    [Authorize(Policy = FilesAuthConstants.ImportPushPolicy)]
    [DisableRequestSizeLimit]
    [DisableFormValueBinding]
    [ProducesResponseType<ApiResponse<FileImportPushResultModel>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiResponse<FileImportPushResultModel>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Push(string id, CancellationToken ct)
    {
        if (!MediaTypeHeaderValue.TryParse(Request.ContentType, out var mediaType) || HeaderUtilities.RemoveQuotes(mediaType.Boundary).Value is not { Length: > 0 } boundary)
            return this.ToProblem(AppErrorFactory.Validation("The push must be multipart/form-data."));

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

            var spec = new FileImportPushSpec
            {
                Source = fields.GetValueOrDefault("source") ?? string.Empty,
                Key = fields.GetValueOrDefault("key") ?? string.Empty,
                Sha256 = fields.GetValueOrDefault("sha256") ?? string.Empty,
                Reference = fields.GetValueOrDefault("reference"),
                Name = fields.GetValueOrDefault("name"),
                ContentType = fields.GetValueOrDefault("contentType"),
                App = fields.GetValueOrDefault("app"),
            };
            var pushed = await imports.PushAsync(id, section.Body, spec, ct);
            return pushed.Match<IActionResult>(
                file => file.Created
                    ? StatusCode(StatusCodes.Status201Created, ApiResponse<FileImportPushResultModel>.Ok(file))
                    : Ok(ApiResponse<FileImportPushResultModel>.Ok(file)),
                error => this.ToProblem(error));
        }

        return this.ToProblem(AppErrorFactory.Validation("The push has no file part."));
    }

    private IActionResult Answer<T>(Result<T> result) where T : notnull =>
        result.Match<IActionResult>(value => Ok(ApiResponse<T>.Ok(value)), error => this.ToProblem(error));
}
