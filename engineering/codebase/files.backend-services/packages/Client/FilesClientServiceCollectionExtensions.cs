using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WoW.Two.Sdk.Beta.Be.Files.Audit;
using WoW.Two.Sdk.Beta.Be.Files.Client.Files;
using WoW.Two.Sdk.Beta.Be.Files.Client.Transport;
using WoW.Two.Sdk.Beta.Be.Files.References;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Files.Uploads;
using WoW.Two.Sdk.Beta.Be.Files.Urls;

namespace WoW.Two.Sdk.Beta.Be.Files.Client;

/// <summary>Registers the Files HTTP client (Forms 2 and 3).</summary>
public static class FilesClientServiceCollectionExtensions
{
    /// <summary>Registers every Files broker over the running Files service, through one named
    /// <see cref="HttpClient"/> that sends the tenant API key when one is set.</summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">Sets the service's root URL and, for key callers, the API key.</param>
    /// <returns>The HTTP client builder, for client-credentials tokens (<c>AddOAuth2ClientCredentials</c>), resilience
    /// handlers or a test server's handler.</returns>
    public static IHttpClientBuilder AddFilesClient(this IServiceCollection services, Action<FilesClientOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<FilesClientOptions>()
            .Configure(configure)
            .Validate(options => options.BaseAddress is { IsAbsoluteUri: true }, "FilesClientOptions.BaseAddress must be an absolute URL.");

        services.AddTransient<FilesApiKeyHandler>();
        services.AddTransient<FilesApiTransport>();
        services.AddTransient<IFileUploadsBroker, ApiFileUploadsBroker>();
        services.AddTransient<IFileStoreBroker, ApiFileStoreBroker>();
        services.AddTransient<IFileReferencesBroker, ApiFileReferencesBroker>();
        services.AddTransient<IFileUrlsBroker, ApiFileUrlsBroker>();
        services.AddTransient<IFileAuditBroker, ApiFileAuditBroker>();

        return services.AddHttpClient(FilesClientConstants.HttpClientName, (provider, http) =>
            {
                var root = provider.GetRequiredService<IOptions<FilesClientOptions>>().Value.BaseAddress!.ToString();
                http.BaseAddress = new Uri(root.EndsWith('/') ? root : root + "/");
            })
            .AddHttpMessageHandler<FilesApiKeyHandler>();
    }
}
