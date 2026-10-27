using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using WoW.Two.Sdk.Beta.Be.Tenancy.Core;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Callers;

/// <summary>Resolves the caller from the request: the tenant a Files host stamped (<c>files_tenant</c>), else the ambient
/// tenant (be sdk tenancy), else the module's default; the subject, organization and app from the user's claims
/// (<c>sub</c>, <c>org_id</c>, <c>azp</c>).</summary>
/// <param name="options">The module settings, for the default tenant.</param>
/// <param name="http">The request accessor; no request means a background call.</param>
/// <param name="tenants">The ambient tenant, when the host runs be sdk tenancy.</param>
internal sealed class HttpFilesCallerService(
    IOptions<FilesModuleOptions> options,
    IHttpContextAccessor? http = null,
    ITenantContext? tenants = null) : IFilesCallerService
{
    /// <inheritdoc />
    public FilesCallerModel Current
    {
        get
        {
            var user = http?.HttpContext?.User;
            return new FilesCallerModel
            {
                TenantId = Claim(user, FilesCallerClaimConstants.Tenant) ?? tenants?.TenantId ?? options.Value.DefaultTenantId,
                SubjectId = Claim(user, "sub") ?? Claim(user, ClaimTypes.NameIdentifier),
                OrganizationId = Claim(user, "org_id"),
                AppId = Claim(user, "azp") ?? Claim(user, "client_id"),
            };
        }
    }

    private static string? Claim(ClaimsPrincipal? user, string type) =>
        user?.Identity?.IsAuthenticated == true ? user.FindFirst(type)?.Value : null;
}
