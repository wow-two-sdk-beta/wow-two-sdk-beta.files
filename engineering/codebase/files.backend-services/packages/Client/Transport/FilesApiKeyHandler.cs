using Microsoft.Extensions.Options;

namespace WoW.Two.Sdk.Beta.Be.Files.Client.Transport;

/// <summary>Adds the tenant API key to every call when the client has one.</summary>
internal sealed class FilesApiKeyHandler(IOptions<FilesClientOptions> options) : DelegatingHandler
{
    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (options.Value.ApiKey is { Length: > 0 } key && !request.Headers.Contains(FilesClientConstants.ApiKeyHeader))
            request.Headers.Add(FilesClientConstants.ApiKeyHeader, key);
        return base.SendAsync(request, cancellationToken);
    }
}
