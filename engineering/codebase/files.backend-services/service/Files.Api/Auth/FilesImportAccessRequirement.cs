using Microsoft.AspNetCore.Authorization;
using WoW.Two.Sdk.Beta.Be.Files.Module.Imports;
using WoW.Two.Sdk.Beta.Be.Identity.CurrentPrincipal;

namespace Files.Api.Auth;

/// <summary>Requires access to the import a route names (<c>{id}</c>): that import's token in
/// <see cref="FileImportConstants.TokenHeader"/>, or the operator when <see cref="AllowsOperator"/>. A presented token
/// decides alone: one for another import, expired or with a bad mac fails, and with no other credential answers 401.</summary>
/// <param name="AllowsOperator">Whether the operator key reaches the route too.</param>
public sealed record FilesImportAccessRequirement(bool AllowsOperator) : IAuthorizationRequirement;

/// <summary>Checks <see cref="FilesImportAccessRequirement"/>: reads the token from the request and the import from the
/// route.</summary>
/// <param name="principal">Who is calling, for the operator.</param>
/// <param name="tokens">Reads import tokens.</param>
internal sealed class FilesImportAccessHandler(ICurrentPrincipal principal, IFileImportTokenService tokens) : AuthorizationHandler<FilesImportAccessRequirement>
{
    /// <summary>Holds the route value that names the import.</summary>
    private const string ImportRouteValue = "id";

    /// <inheritdoc />
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, FilesImportAccessRequirement requirement)
    {
        if (context.Resource is not HttpContext http)
            return Task.CompletedTask;

        var importId = http.GetRouteValue(ImportRouteValue) as string;
        if (http.Request.Headers.TryGetValue(FileImportConstants.TokenHeader, out var presented))
        {
            if (importId is not null && tokens.Read(presented.ToString().Trim()) is { } claims
                && string.Equals(claims.ImportId, importId, StringComparison.Ordinal))
                context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (requirement.AllowsOperator && FilesOperatorRequirement.IsOperator(principal))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
