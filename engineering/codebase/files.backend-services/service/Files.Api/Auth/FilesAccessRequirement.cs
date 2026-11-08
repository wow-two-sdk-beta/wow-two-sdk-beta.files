using Microsoft.AspNetCore.Authorization;
using WoW.Two.Sdk.Beta.Be.Identity.CurrentPrincipal;

namespace Files.Api.Auth;

/// <summary>Requires what a Files action needs: the app's scope always, and a person's permission beside it (t8 § Policy
/// shape). A service or an API key holds no person, so its scope alone answers.</summary>
/// <param name="Scope">The scope the calling app must hold, such as <c>files.read</c>.</param>
/// <param name="Permission">The permission a person must hold, such as <c>files:read</c>.</param>
public sealed record FilesAccessRequirement(string Scope, string Permission) : IAuthorizationRequirement
{
    /// <summary>Tells whether a principal may take the action; record rules (owner, organization, visibility) run later,
    /// against Files' own rows.</summary>
    public bool Allows(ICurrentPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        return principal.Kind switch
        {
            PrincipalKind.Service or PrincipalKind.ApiKey => principal.Scopes.Contains(Scope),
            PrincipalKind.Person => principal.Scopes.Contains(Scope) && principal.Permissions.Contains(Permission),
            _ => false,
        };
    }
}

/// <summary>Checks <see cref="FilesAccessRequirement"/> against the one principal, so no policy branches by scheme.</summary>
/// <param name="principal">Who is calling.</param>
internal sealed class FilesAccessHandler(ICurrentPrincipal principal) : AuthorizationHandler<FilesAccessRequirement>
{
    /// <inheritdoc />
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, FilesAccessRequirement requirement)
    {
        if (requirement.Allows(principal))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
