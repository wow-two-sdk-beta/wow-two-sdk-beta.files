using Microsoft.Extensions.Options;
using WoW.Two.Sdk.Beta.Be.Identity.CurrentPrincipal;
using WoW.Two.Sdk.Beta.Be.Tenancy.Core;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Callers;

/// <summary>Resolves the caller from the kernel principal: the tenant its trusted issuer or API key stamped
/// (<see cref="ICurrentPrincipal.Partition"/>), else the ambient tenant (be sdk tenancy), else the module's default; the
/// subject, issuer, organization, app and actor from the principal.</summary>
/// <param name="options">The module settings, for the default tenant.</param>
/// <param name="principal">Who is calling; none when the host registers no principal, which reads as a background call.</param>
/// <param name="tenants">The ambient tenant, when the host runs be sdk tenancy.</param>
internal sealed class PrincipalFilesCallerService(
    IOptions<FilesModuleOptions> options,
    ICurrentPrincipal? principal = null,
    ITenantContext? tenants = null) : IFilesCallerService
{
    /// <inheritdoc />
    public FilesCallerModel Current
    {
        get
        {
            var known = principal is { Kind: not PrincipalKind.Anonymous } ? principal : null;
            return new FilesCallerModel
            {
                TenantId = known?.Partition ?? tenants?.TenantId ?? options.Value.DefaultTenantId,
                Kind = known?.Kind ?? PrincipalKind.Anonymous,
                SubjectId = known?.Subject,
                Issuer = known?.Issuer,
                OrganizationId = known?.OrganizationId,
                AppId = known?.ClientId,
                ActorId = known?.Actor?.Subject,
            };
        }
    }
}
