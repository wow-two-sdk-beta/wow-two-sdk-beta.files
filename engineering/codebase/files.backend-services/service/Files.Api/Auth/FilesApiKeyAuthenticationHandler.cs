using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using WoW.Two.Sdk.Beta.Be.Files.Module.Callers;

namespace Files.Api.Auth;

/// <summary>Authenticates a tenant API key from <c>X-Api-Key</c> against the configured keys' SHA-256, and stamps the
/// key's tenant, app and scopes on the caller.</summary>
public sealed class FilesApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptionsMonitor<FilesAuthSettings> settings) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Request.Headers[FilesAuthConstants.ApiKeyHeader].FirstOrDefault() is not { Length: > 0 } presented)
            return Task.FromResult(AuthenticateResult.NoResult());

        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(presented));
        var key = settings.CurrentValue.ApiKeys.FirstOrDefault(candidate =>
            candidate.Sha256.Length == 64 && CryptographicOperations.FixedTimeEquals(digest, Convert.FromHexString(candidate.Sha256)));
        if (key is null)
            return Task.FromResult(AuthenticateResult.Fail("The API key is not known."));

        var claims = new List<Claim>
        {
            new("sub", key.Id),
            new(FilesCallerClaimConstants.Tenant, key.TenantId),
        };
        if (key.AppId is not null)
            claims.Add(new Claim("azp", key.AppId));
        claims.AddRange(key.Scopes.Select(scope => new Claim("scope", scope)));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name, "sub", ClaimTypes.Role));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }
}
