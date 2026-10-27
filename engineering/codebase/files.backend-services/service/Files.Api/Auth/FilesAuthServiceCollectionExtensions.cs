using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using WoW.Two.Sdk.Beta.Be.Files;
using WoW.Two.Sdk.Beta.Be.Files.Module.Callers;

namespace Files.Api.Auth;

/// <summary>Registers how Files API callers authenticate and what each scope reaches.</summary>
public static class FilesAuthServiceCollectionExtensions
{
    private static readonly string[] Scopes =
    [
        FilesScopeConstants.Read, FilesScopeConstants.Write, FilesScopeConstants.Share,
        FilesScopeConstants.Admin, FilesScopeConstants.Delegate, FilesScopeConstants.Tenants,
    ];

    /// <summary>Registers the API key scheme, the token scheme over every trusted issuer in <c>Files:Auth</c>, the
    /// policy scheme that picks between them, and one authorization policy per Files scope.</summary>
    /// <remarks>Issuers and keys are read when first used and reload live, so a host's late configuration (a test
    /// host's included) reaches them. A token's issuer decides its tenant and the audience it must name.</remarks>
    public static IServiceCollection AddFilesAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FilesAuthSettings>(configuration.GetSection(FilesAuthSettings.SectionName));
        services.AddSingleton<FilesIssuerKeyResolver>();

        services.AddAuthentication(FilesAuthConstants.Scheme)
            .AddPolicyScheme(FilesAuthConstants.Scheme, "Files", options => options.ForwardDefaultSelector = SelectScheme)
            .AddScheme<AuthenticationSchemeOptions, FilesApiKeyAuthenticationHandler>(FilesAuthConstants.ApiKeyScheme, null)
            .AddJwtBearer(FilesAuthConstants.TokenScheme);

        services.AddOptions<JwtBearerOptions>(FilesAuthConstants.TokenScheme)
            .Configure<IOptionsMonitor<FilesAuthSettings>, FilesIssuerKeyResolver>((options, settings, keys) =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "sub",
                    IssuerValidator = (issuer, _, _) => FindIssuer(settings.CurrentValue, issuer) is not null
                        ? issuer
                        : throw new SecurityTokenInvalidIssuerException($"The issuer '{issuer}' is not trusted.") { InvalidIssuer = issuer },
                    AudienceValidator = (audiences, token, _) =>
                        FindIssuer(settings.CurrentValue, token.Issuer) is { } trusted && audiences.Contains(trusted.Audience, StringComparer.Ordinal),
                    IssuerSigningKeyResolver = (_, token, _, _) =>
                        FindIssuer(settings.CurrentValue, token.Issuer) is { } trusted ? keys.Resolve(trusted) : [],
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        // The issuer decides the tenant; a token's own claims never choose it.
                        var trusted = FindIssuer(settings.CurrentValue, context.SecurityToken.Issuer);
                        if (trusted is null || context.Principal?.Identity is not ClaimsIdentity identity)
                        {
                            context.Fail("The token's issuer is not trusted.");
                            return Task.CompletedTask;
                        }

                        foreach (var claim in identity.FindAll(FilesCallerClaimConstants.Tenant).ToList())
                            identity.RemoveClaim(claim);
                        identity.AddClaim(new Claim(FilesCallerClaimConstants.Tenant, trusted.TenantId));
                        return Task.CompletedTask;
                    },
                };
            });

        services.AddAuthorization(options =>
        {
            foreach (var scope in Scopes)
                options.AddPolicy(scope, policy => policy.RequireAuthenticatedUser().RequireAssertion(context => HasScope(context.User, scope)));
        });
        return services;
    }

    /// <summary>Tells whether the caller holds a scope, from repeated <c>scope</c> claims or one space-delimited claim.</summary>
    internal static bool HasScope(ClaimsPrincipal user, string scope) =>
        user.FindAll("scope").Concat(user.FindAll("scp"))
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Contains(scope, StringComparer.Ordinal);

    private static FilesIssuerSettings? FindIssuer(FilesAuthSettings settings, string? issuer) =>
        settings.Issuers.FirstOrDefault(trusted => string.Equals(trusted.Issuer, issuer, StringComparison.Ordinal));

    /// <summary>Picks the API key scheme for a key, the token scheme for a bearer token.</summary>
    private static string SelectScheme(HttpContext context) =>
        !context.Request.Headers.ContainsKey(FilesAuthConstants.ApiKeyHeader)
        && context.Request.Headers.Authorization.FirstOrDefault() is { } authorization
        && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? FilesAuthConstants.TokenScheme
            : FilesAuthConstants.ApiKeyScheme;
}
