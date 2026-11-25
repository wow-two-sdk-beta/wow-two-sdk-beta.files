using Files.Application.Status.Queries;
using WoW.Two.Sdk.Beta.Be.Mediator;
using WoW.Two.Sdk.Beta.Be.Web.ExceptionHandling.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Files.Api.Status.Models;
using WoW.Two.Sdk.Beta.Be.Web.Contracts;

namespace Files.Api.Status.Controllers;

/// <summary>Exposes system status over HTTP.</summary>
[ApiController]
[Route("api/system")]
public sealed class SystemController(ISender sender) : ControllerBase
{
    /// <summary>Holds the name the service reports.</summary>
    private const string ServiceName = "fss";

    /// <summary>Reports service liveness.</summary>
    /// <returns>The service name, liveness state and running version.</returns>
    [HttpGet("status")]
    [AllowAnonymous]
    [ProducesResponseType<ApiResponse<SystemStatusDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Status(CancellationToken ct)
    {
        var result = await sender.SendAsync(new GetFilesSystemStatusQuery(), ct);

        return result.Match<IActionResult>(success => Ok(ApiResponse<SystemStatusDto>.Ok(new SystemStatusDto
        {
            Service = ServiceName,
            Status = "ok",
            Version = success.Data.Version,
            Imaging = success.Data.Imaging,
        })), failure => this.ToProblem(failure.Error));
    }
}
