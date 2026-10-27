using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;
using WoW.Two.Sdk.Beta.Be.Files.Client.Transport;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Files.Urls;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;

namespace WoW.Two.Sdk.Beta.Be.Files.Client.Files;

/// <summary>Integrates the running Files service's URLs.</summary>
internal sealed class ApiFileUrlsBroker(FilesApiTransport transport) : IFileUrlsBroker
{
    /// <inheritdoc />
    public Task<Result<SignedFileUrl>> IssueAsync(FileId id, UrlSpec spec, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(spec);

        var request = new HttpRequestMessage(HttpMethod.Post, FilesApiRouteMapper.File(id, "urls"))
        {
            Content = JsonContent.Create(spec, options: FilesApiTransport.Json),
        };
        return transport.SendAsync<SignedFileUrl>(request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result<Uri>> GetPublicUrlAsync(FileId id, string? variant = null, CancellationToken cancellationToken = default)
    {
        var path = FilesApiRouteMapper.File(id, "public-url");
        if (variant is not null)
            path = QueryHelpers.AddQueryString(path, "variant", variant);
        return transport.SendAsync<Uri>(new HttpRequestMessage(HttpMethod.Get, path), cancellationToken);
    }
}
