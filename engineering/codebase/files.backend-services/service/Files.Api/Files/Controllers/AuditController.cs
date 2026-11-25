using Files.Api.Auth;
using Files.Application.Audit.Queries;
using WoW.Two.Sdk.Beta.Be.Files.Module.Callers;
using WoW.Two.Sdk.Beta.Be.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Files.Api.Files.Mappers;
using WoW.Two.Sdk.Beta.Be.Files;
using WoW.Two.Sdk.Beta.Be.Files.Audit;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;
using WoW.Two.Sdk.Beta.Be.Web.Contracts;
using WoW.Two.Sdk.Beta.Be.Web.ExceptionHandling.Extensions;

namespace Files.Api.Files.Controllers;

/// <summary>Exposes the tenant's audit trail over HTTP; it takes <c>files.admin</c>.</summary>
[ApiController]
[Route(FilesRouteConstants.Api + "/audit")]
[Authorize(Policy = FilesScopeConstants.Admin)]
public sealed class AuditController(ISender sender, IFilesCallerService caller) : ControllerBase
{
    /// <summary>Lists the tenant's audit entries, newest first.</summary>
    [HttpGet]
    [ProducesResponseType<ApiResponse<FileAuditPage>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] string? fileId, [FromQuery] string? action, [FromQuery] string? cursor, [FromQuery] int? limit, CancellationToken ct)
    {
        FileId? file = null;
        if (fileId is not null)
        {
            if (!FileId.TryParse(fileId, out var parsed))
                return this.ToProblem(AppErrorFactory.Validation("The file id is not a UUID."));
            file = parsed;
        }

        if (!WireEnumMapper.TryParse<FileAuditAction>(action, out var auditAction))
            return this.ToProblem(AppErrorFactory.Validation("The action filter is not known."));

        var criteria = new FileAuditQuery { FileId = file, Action = auditAction, Cursor = cursor, Limit = limit ?? 50 };
        var result = await sender.SendAsync(new GetAllFileAuditEntriesQuery(caller.ToActor(), criteria), ct);

        return result.Match<IActionResult>(success => Ok(ApiResponse<FileAuditPage>.Ok(success.Data)), failure => this.ToProblem(failure.Error));
    }
}
