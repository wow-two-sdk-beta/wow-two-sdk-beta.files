using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using WoW.Two.Sdk.Beta.Be.Identity.Jwt;

namespace Files.Api.Auth;

/// <summary>Finds a token's issuer among the tenants in <c>Files:Auth:Issuers</c>: the tenant it stamps, the audiences its
/// tokens may name, and its keys (t8 C8).</summary>
/// <remarks>Read on each new issuer, so configuration a host adds late still counts. P2 reads the tenants table instead,
/// so a tenant added at runtime takes no restart.</remarks>
/// <param name="settings">The API's caller settings.</param>
internal sealed class FilesTenantIssuerResolver(IOptionsMonitor<FilesAuthSettings> settings) : ITrustedIssuerResolver
{
    private static readonly ConcurrentDictionary<string, IReadOnlyList<SecurityKey>> KeySets = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public ValueTask<TrustedIssuer?> ResolveAsync(string issuer, CancellationToken cancellationToken)
    {
        var current = settings.CurrentValue;
        var trusted = current.Issuers.FirstOrDefault(candidate => string.Equals(candidate.Issuer, issuer, StringComparison.Ordinal));
        return ValueTask.FromResult(trusted is null ? null : ToTrustedIssuer(trusted, current));
    }

    /// <summary>Maps an issuer row onto what the token scheme checks; null when it names no audience at all, which no
    /// token could match.</summary>
    internal static TrustedIssuer? ToTrustedIssuer(FilesIssuerSettings issuer, FilesAuthSettings settings)
    {
        var audiences = AudiencesOf(issuer, settings);
        if (audiences.Count == 0 || string.IsNullOrWhiteSpace(issuer.TenantId))
            return null;

        return new TrustedIssuer
        {
            Issuer = issuer.Issuer,
            MetadataAddress = issuer.MetadataAddress,
            SigningKeys = issuer.Jwks is { Length: > 0 } jwks ? KeySets.GetOrAdd(jwks, static json => [.. new JsonWebKeySet(json).GetSigningKeys()]) : null,
            Audiences = audiences,
            Partition = issuer.TenantId,
        };
    }

    /// <summary>Lists the audiences an issuer's tokens may name: its own, else the instance's.</summary>
    internal static IReadOnlyList<string> AudiencesOf(FilesIssuerSettings issuer, FilesAuthSettings settings) =>
        issuer.Audiences.Count > 0 ? issuer.Audiences
        : settings.Audience is { Length: > 0 } audience ? [audience]
        : [];
}
