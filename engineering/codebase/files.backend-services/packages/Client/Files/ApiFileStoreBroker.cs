using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using WoW.Two.Sdk.Beta.Be.Files.Client.Transport;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;

namespace WoW.Two.Sdk.Beta.Be.Files.Client.Files;

/// <summary>Integrates the running Files service's store: metadata, content with one byte range, list, change, delete
/// and restore.</summary>
internal sealed class ApiFileStoreBroker(FilesApiTransport transport) : IFileStoreBroker
{
    /// <inheritdoc />
    public Task<Result<FileModel>> GetAsync(FileId id, CancellationToken cancellationToken = default) =>
        transport.SendAsync<FileModel>(new HttpRequestMessage(HttpMethod.Get, FilesApiRouteMapper.File(id)), cancellationToken);

    /// <inheritdoc />
    public async Task<Result<FileContent>> OpenReadAsync(FileId id, ByteRange? range = null, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, FilesApiRouteMapper.File(id, "content"));
        if (range is { } asked)
            request.Headers.Range = new RangeHeaderValue(asked.From, asked.To);

        var sent = await transport.SendRawAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (sent is Result<HttpResponseMessage>.Failure failure)
            return Result<FileContent>.Fail(failure.Error);

        var response = ((Result<HttpResponseMessage>.Success)sent).Value;
        var headers = response.Content.Headers;
        var length = headers.ContentLength ?? 0;
        var partial = response.StatusCode == HttpStatusCode.PartialContent && headers.ContentRange is { From: { } from, To: { } to };
        var served = partial ? new ByteRange(headers.ContentRange!.From!.Value, headers.ContentRange.To) : (ByteRange?)null;
        var total = partial ? headers.ContentRange!.Length ?? length : length;
        var stream = new ResponseOwningStream(await response.Content.ReadAsStreamAsync(cancellationToken), response);

        return Result<FileContent>.Ok(new FileContent(stream, headers.ContentType?.MediaType ?? "application/octet-stream", length, total, served));
    }

    /// <inheritdoc />
    public Task<Result<FilePage>> ListAsync(FileQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var parameters = new List<KeyValuePair<string, string?>>
        {
            new("limit", query.Limit.ToString(CultureInfo.InvariantCulture)),
        };
        if (query.AppId is not null)
            parameters.Add(new("app", query.AppId));
        if (query.OwnerId is not null)
            parameters.Add(new("owner", query.OwnerId));
        if (query.Kind is { } kind)
            parameters.Add(new("kind", FilesApiRouteMapper.WireName(kind)));
        if (query.State is { } state)
            parameters.Add(new("state", FilesApiRouteMapper.WireName(state)));
        if (query.CreatedAfterUtc is { } createdAfter)
            parameters.Add(new("createdAfter", createdAfter.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)));
        if (query.IncludeDeleted)
            parameters.Add(new("includeDeleted", "true"));
        if (query.Cursor is not null)
            parameters.Add(new("cursor", query.Cursor));

        return transport.SendAsync<FilePage>(new HttpRequestMessage(HttpMethod.Get, FilesApiRouteMapper.Files(parameters)), cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result<FileModel>> UpdateAsync(FileId id, FilePatch patch, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(patch);

        var request = new HttpRequestMessage(HttpMethod.Patch, FilesApiRouteMapper.File(id))
        {
            Content = JsonContent.Create(patch, options: FilesApiTransport.Json),
        };
        return transport.SendAsync<FileModel>(request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result<FileModel>> DeleteAsync(FileId id, CancellationToken cancellationToken = default) =>
        transport.SendAsync<FileModel>(new HttpRequestMessage(HttpMethod.Delete, FilesApiRouteMapper.File(id)), cancellationToken);

    /// <inheritdoc />
    public Task<Result<FileModel>> RestoreAsync(FileId id, CancellationToken cancellationToken = default) =>
        transport.SendAsync<FileModel>(new HttpRequestMessage(HttpMethod.Post, FilesApiRouteMapper.File(id, "restore")), cancellationToken);

    /// <summary>Reads a response body and disposes the response with it.</summary>
    private sealed class ResponseOwningStream(Stream inner, HttpResponseMessage response) : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            inner.ReadAsync(buffer, cancellationToken);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            inner.ReadAsync(buffer, offset, count, cancellationToken);

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
                response.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
