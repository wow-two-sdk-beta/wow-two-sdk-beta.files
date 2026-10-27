using System.Collections.Concurrent;
using System.Text;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Files.Api.Auth;

/// <summary>Resolves a trusted issuer's signing keys: its symmetric dev key, or the keys its OIDC discovery publishes,
/// cached per authority and refreshed as the configuration manager decides.</summary>
public sealed class FilesIssuerKeyResolver
{
    private readonly ConcurrentDictionary<string, ConfigurationManager<OpenIdConnectConfiguration>> discovery = new(StringComparer.Ordinal);

    /// <summary>Resolves the keys of an issuer.</summary>
    /// <param name="issuer">The issuer's settings.</param>
    /// <returns>The keys tokens from the issuer may be signed with.</returns>
    public IEnumerable<SecurityKey> Resolve(FilesIssuerSettings issuer)
    {
        ArgumentNullException.ThrowIfNull(issuer);

        if (issuer.SigningKey is { Length: > 0 } key)
            return [new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))];

        var authority = (issuer.Authority ?? issuer.Issuer).TrimEnd('/');
        var manager = discovery.GetOrAdd(authority, static root => new ConfigurationManager<OpenIdConnectConfiguration>(
            $"{root}/.well-known/openid-configuration",
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever { RequireHttps = root.StartsWith("https://", StringComparison.OrdinalIgnoreCase) }));

        // The handler asks synchronously; the first call per authority waits for discovery, later ones read the cache.
        return manager.GetConfigurationAsync(CancellationToken.None).GetAwaiter().GetResult().SigningKeys;
    }
}
