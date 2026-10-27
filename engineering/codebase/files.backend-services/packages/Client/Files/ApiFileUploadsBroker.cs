using System.Globalization;
using System.Net.Http.Headers;
using WoW.Two.Sdk.Beta.Be.Files.Client.Transport;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Files.Uploads;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;

namespace WoW.Two.Sdk.Beta.Be.Files.Client.Files;

/// <summary>Integrates the running Files service's uploads: a streamed multipart <c>POST</c>, fields before the file.</summary>
internal sealed class ApiFileUploadsBroker(FilesApiTransport transport) : IFileUploadsBroker
{
    /// <inheritdoc />
    public Task<Result<FileModel>> UploadAsync(Stream content, UploadSpec spec, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(spec);

        var form = new MultipartFormDataContent
        {
            { new StringContent(spec.Name), "name" },
            { new StringContent(FilesApiRouteMapper.WireName(spec.Visibility)), "visibility" },
        };
        if (spec.AppId is not null)
            form.Add(new StringContent(spec.AppId), "appId");
        if (spec.ContentType is not null)
            form.Add(new StringContent(spec.ContentType), "contentType");
        if (spec.ExpiresAtUtc is { } expiresAt)
            form.Add(new StringContent(expiresAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)), "expiresAtUtc");

        var file = new StreamContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(spec.ContentType ?? "application/octet-stream");
        form.Add(file, "file", spec.Name);

        var request = new HttpRequestMessage(HttpMethod.Post, FilesApiRouteMapper.Files()) { Content = form };
        if (!string.IsNullOrWhiteSpace(spec.IdempotencyKey))
            request.Headers.Add(FilesRouteConstants.IdempotencyKeyHeader, spec.IdempotencyKey);
        return transport.SendAsync<FileModel>(request, cancellationToken);
    }
}
