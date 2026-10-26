using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WoW.Two.Sdk.Beta.Be.Files.Client.Greetings;
using WoW.Two.Sdk.Beta.Be.Files.Greetings;

namespace WoW.Two.Sdk.Beta.Be.Files.Client;

/// <summary>Registers the Files HTTP client (Forms 2 and 3).</summary>
public static class FilesClientServiceCollectionExtensions
{
    /// <summary>Registers <see cref="IGreetingsBroker"/> over the running Files service.</summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">Sets the service's root URL.</param>
    /// <returns>The HTTP client builder, for resilience handlers or a test server's handler.</returns>
    public static IHttpClientBuilder AddFilesClient(this IServiceCollection services, Action<FilesClientOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<FilesClientOptions>()
            .Configure(configure)
            .Validate(options => options.BaseAddress is { IsAbsoluteUri: true }, "FilesClientOptions.BaseAddress must be an absolute URL.");

        return services.AddHttpClient<IGreetingsBroker, ApiGreetingsBroker>((provider, http) =>
        {
            var root = provider.GetRequiredService<IOptions<FilesClientOptions>>().Value.BaseAddress!.ToString();
            http.BaseAddress = new Uri(root.EndsWith('/') ? root : root + "/");
        });
    }
}
