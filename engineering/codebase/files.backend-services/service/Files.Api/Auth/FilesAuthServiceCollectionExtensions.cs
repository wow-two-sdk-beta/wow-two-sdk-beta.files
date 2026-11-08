using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using WoW.Two.Sdk.Beta.Be.Files;
using WoW.Two.Sdk.Beta.Be.Identity.ApiKeys;
using WoW.Two.Sdk.Beta.Be.Identity.CurrentUser;
using WoW.Two.Sdk.Beta.Be.Identity.Jwt;
using WoW.Two.Sdk.Beta.Be.Identity.Signals;

namespace Files.Api.Auth;

/// <summary>Registers how Files API callers authenticate, what each action needs, and how revoked tokens stop counting.</summary>
public static class FilesAuthServiceCollectionExtensions
{
    /// <summary>Holds each action's scope and the permission a person needs beside it.</summary>
    private static readonly (string Scope, string Permission)[] Actions =
    [
        (FilesScopeConstants.Read, FilesPermissionConstants.Read),
        (FilesScopeConstants.Write, FilesPermissionConstants.Write),
        (FilesScopeConstants.Share, FilesPermissionConstants.Share),
        (FilesScopeConstants.Admin, FilesPermissionConstants.Admin),
    ];

    /// <summary>Registers Identity's token scheme over the trusted issuers in <c>Files:Auth</c>, its API key scheme over the
    /// configured keys, the policy scheme that picks between them, the one principal, a policy per action named by its
    /// scope, and Identity's revocation signals with the deny list (t8 C1, C8, C9, C12).</summary>
    /// <param name="builder">The host builder.</param>
    public static WebApplicationBuilder AddFilesAuth(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var services = builder.Services;
        var isDevelopment = builder.Environment.IsDevelopment();

        services.Configure<FilesAuthSettings>(builder.Configuration.GetSection(FilesAuthSettings.SectionName));

        // Tokens: the resolver reads each issuer from the settings when first seen, with its own audiences and tenant.
        services.AddJwtBearerAuthentication(options =>
        {
            options.Audience = FilesAuthConstants.UnsetAudience;
            options.IsInsecureMetadataAllowedInDevelopment = isDevelopment;
            options.UseTrustedIssuerResolver<FilesTenantIssuerResolver>();
        });

        // API keys: only for callers outside Identity, such as a shared host's tenants without an Identity instance.
        services.AddApiKeyAuthentication(options =>
        {
            options.Marker = FilesAuthConstants.ApiKeyMarker;
            options.HeaderName = FilesAuthConstants.ApiKeyHeader;
        });
        services.AddScoped<IApiKeyRepository, FilesApiKeyRepository>();

        // Registered after both schemes, so this default wins over the bearer default Identity sets on a host that names none.
        services.AddAuthentication(FilesAuthConstants.Scheme)
            .AddPolicyScheme(FilesAuthConstants.Scheme, "Files", options => options.ForwardDefaultSelector = SelectScheme);

        services.AddCurrentPrincipal();
        services.AddSingleton<IAuthorizationHandler, FilesAccessHandler>();
        services.AddAuthorization(options =>
        {
            foreach (var (scope, permission) in Actions)
                options.AddPolicy(scope, policy => policy.RequireAuthenticatedUser().AddRequirements(new FilesAccessRequirement(scope, permission)));
        });

        // Revocation: one route under each tenant's host hears its issuer; a signal refuses that person's tokens until they expire.
        services.AddIdentitySignalReceiver();
        services.AddOptions<IdentitySignalReceiverOptions>()
            .Configure<IOptionsMonitor<FilesAuthSettings>>((options, settings) => ConfigureSignals(options, settings.CurrentValue, isDevelopment));
        services.AddTokenDenyList();

        return builder;
    }

    /// <summary>Maps the signal route when the settings name an issuer and a signal URL.</summary>
    /// <param name="app">The built application.</param>
    public static WebApplication MapFilesSignals(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var signals = app.Services.GetRequiredService<IOptions<IdentitySignalReceiverOptions>>().Value;
        if (signals.TrustedIssuers.Count > 0 && signals.Audiences.Count > 0)
            app.MapIdentitySignals("/" + FilesAuthConstants.SignalPath);
        return app;
    }

    /// <summary>Lists every issuer's keys and every tenant host's signal URL, <c>{audience}signals/identity</c>.</summary>
    internal static void ConfigureSignals(IdentitySignalReceiverOptions options, FilesAuthSettings settings, bool isDevelopment)
    {
        options.IsInsecureMetadataAllowedInDevelopment = isDevelopment;
        foreach (var issuer in settings.Issuers)
        {
            if (FilesTenantIssuerResolver.ToTrustedIssuer(issuer, settings) is not { } trusted)
                continue;

            options.TrustedIssuers.Add(trusted with { Audiences = [], Partition = null });
            foreach (var audience in trusted.Audiences)
            {
                var url = SignalUrlOf(audience);
                if (!options.Audiences.Contains(url, StringComparer.Ordinal))
                    options.Audiences.Add(url);
            }
        }
    }

    /// <summary>Builds the signal URL under a resource URI, which keeps its trailing slash.</summary>
    internal static string SignalUrlOf(string audience) =>
        (audience.EndsWith('/') ? audience : audience + "/") + FilesAuthConstants.SignalPath;

    /// <summary>Picks the API key scheme for a key, in its header or as a bearer token with the Files marker, and the token
    /// scheme for any other bearer token.</summary>
    private static string SelectScheme(HttpContext context)
    {
        if (context.Request.Headers.ContainsKey(FilesAuthConstants.ApiKeyHeader))
            return ApiKeyAuthenticationDefaults.Scheme;

        return context.Request.Headers.Authorization.FirstOrDefault() is { } authorization
            && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            && !authorization.AsSpan("Bearer ".Length).TrimStart().StartsWith(FilesAuthConstants.ApiKeyMarker, StringComparison.Ordinal)
                ? JwtBearerDefaults.AuthenticationScheme
                : ApiKeyAuthenticationDefaults.Scheme;
    }
}
