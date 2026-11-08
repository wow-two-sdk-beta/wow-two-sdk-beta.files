using Microsoft.AspNetCore.Authorization;
using WoW.Two.Sdk.Beta.Be.Files;
using WoW.Two.Sdk.Beta.Be.Identity.CurrentPrincipal;

namespace Files.Api.Auth;

/// <summary>Requires the operator: the operator key, holding <c>files.tenants</c> and no tenant. A person, a service or a
/// tenant's key never meets it, whatever scopes it carries.</summary>
public sealed class FilesOperatorRequirement : IAuthorizationRequirement
{
    /// <summary>Tells whether a principal is the operator.</summary>
    public static bool IsOperator(ICurrentPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        return principal.Kind == PrincipalKind.ApiKey
            && string.Equals(principal.Subject, FilesAuthConstants.OperatorKeyId, StringComparison.Ordinal)
            && principal.Partition is null
            && principal.Scopes.Contains(FilesScopeConstants.Tenants);
    }
}

/// <summary>Checks <see cref="FilesOperatorRequirement"/> against the one principal.</summary>
/// <param name="principal">Who is calling.</param>
internal sealed class FilesOperatorHandler(ICurrentPrincipal principal) : AuthorizationHandler<FilesOperatorRequirement>
{
    /// <inheritdoc />
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, FilesOperatorRequirement requirement)
    {
        if (FilesOperatorRequirement.IsOperator(principal))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
