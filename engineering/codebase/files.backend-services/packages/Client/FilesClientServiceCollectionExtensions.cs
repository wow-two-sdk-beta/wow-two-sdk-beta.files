using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WoW.Two.Sdk.Beta.Be.Files.Audit;
using WoW.Two.Sdk.Beta.Be.Files.Client.Files;
using WoW.Two.Sdk.Beta.Be.Files.Client.Transport;
using WoW.Two.Sdk.Beta.Be.Files.References;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Files.Uploads;
using WoW.Two.Sdk.Beta.Be.Files.Urls;
using WoW.Two.Sdk.Beta.Be.Http.Auth.OAuth2ClientCredentials;
using WoW.Two.Sdk.Beta.Be.Http.Auth.OAuth2TokenExchange;

namespace WoW.Two.Sdk.Beta.Be.Files.Client;

/// <summary>Registers the Files HTTP client (Forms 2 and 3).</summary>
public static class FilesClientServiceCollectionExtensions
{
    /// <summary>Registers every Files broker over the running Files service, through one named <see cref="HttpClient"/> that
    /// sends the tenant API key when one is set; with <see cref="FilesClientOptions.Identity"/> set, each call first takes
    /// an Identity token instead.</summary>
    /// <remarks>With Identity, a call exchanges the person's token when <see cref="IOAuth2SubjectTokenSource"/> finds a
    /// person on it, by default the current request's bearer token, and sends the service's client-credentials token
    /// otherwise or under <see cref="FilesClientCalls.AsService"/>. The token clients
    /// (<see cref="FilesClientConstants.ServiceHttpClientName"/>, <see cref="FilesClientConstants.PersonHttpClientName"/>)
    /// carry only the token handler and end in the returned client's handlers.</remarks>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">Sets the service's root URL and the API key or the Identity client.</param>
    /// <returns>The HTTP client builder every call ends in, for resilience handlers, a test server's handler or, with
    /// neither an API key nor Identity, the caller's own auth handler.</returns>
    public static IHttpClientBuilder AddFilesClient(this IServiceCollection services, Action<FilesClientOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<FilesClientOptions>()
            .Configure(configure)
            .Validate(options => options.BaseAddress is { IsAbsoluteUri: true }, "FilesClientOptions.BaseAddress must be an absolute URL.")
            .Validate(options => string.IsNullOrEmpty(options.ApiKey) || options.Identity is null, "FilesClientOptions: set ApiKey or Identity, not both.")
            .Validate(options => options.Identity is null or { TokenEndpoint.IsAbsoluteUri: true }, "FilesClientOptions.Identity.TokenEndpoint must be an absolute URL.")
            .Validate(options => options.Identity is null || !string.IsNullOrWhiteSpace(options.Identity.ClientId), "FilesClientOptions.Identity.ClientId is required.")
            .Validate(
                options => options.Identity is null
                    || string.IsNullOrWhiteSpace(options.Identity.ClientSecret) != string.IsNullOrWhiteSpace(options.Identity.ClientPrivateKey),
                "FilesClientOptions.Identity: set ClientSecret or ClientPrivateKey, exactly one.");

        services.AddTransient<FilesApiKeyHandler>();
        services.AddTransient<FilesApiTransport>();
        services.AddTransient<IFileUploadsBroker, ApiFileUploadsBroker>();
        services.AddTransient<IFileStoreBroker, ApiFileStoreBroker>();
        services.AddTransient<IFileReferencesBroker, ApiFileReferencesBroker>();
        services.AddTransient<IFileUrlsBroker, ApiFileUrlsBroker>();
        services.AddTransient<IFileAuditBroker, ApiFileAuditBroker>();

        // The token clients read the Identity settings when first built; without Identity they are never created.
        services.AddOptions<OAuth2ClientCredentialsOptions>(FilesClientConstants.ServiceHttpClientName)
            .Configure<IOptions<FilesClientOptions>>((target, files) => files.Value.Identity?.CopyTo(target, files.Value.BaseAddress!));
        services.AddOptions<OAuth2TokenExchangeOptions>(FilesClientConstants.PersonHttpClientName)
            .Configure<IOptions<FilesClientOptions>>((target, files) => files.Value.Identity?.CopyTo(target, files.Value.BaseAddress!));

        AddTokenClient(services, FilesClientConstants.ServiceHttpClientName).AddOAuth2ClientCredentials(_ => { });
        AddTokenClient(services, FilesClientConstants.PersonHttpClientName).AddOAuth2TokenExchange(_ => { });

        return services.AddHttpClient(FilesClientConstants.HttpClientName, SetRoot)
            .AddHttpMessageHandler<FilesApiKeyHandler>();
    }

    /// <summary>Registers a token client: the service root, and the Files client's own pipeline after its token handler.</summary>
    private static IHttpClientBuilder AddTokenClient(IServiceCollection services, string name) =>
        services.AddHttpClient(name, SetRoot)
            .ConfigurePrimaryHttpMessageHandler(provider => new FilesPipelineHandler(provider.GetRequiredService<IHttpMessageHandlerFactory>()));

    /// <summary>Points a client at the service root, with its trailing slash.</summary>
    private static void SetRoot(IServiceProvider provider, HttpClient http) =>
        http.BaseAddress = new Uri(FilesClientOptions.Root(provider.GetRequiredService<IOptions<FilesClientOptions>>().Value.BaseAddress!));
}
