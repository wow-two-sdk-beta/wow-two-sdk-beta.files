using System.Globalization;
using WoW.Two.Sdk.Beta.Be.Files.Audit;
using WoW.Two.Sdk.Beta.Be.Files.Client.Transport;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;

namespace WoW.Two.Sdk.Beta.Be.Files.Client.Files;

/// <summary>Integrates the running Files service's audit trail.</summary>
internal sealed class ApiFileAuditBroker(FilesApiTransport transport) : IFileAuditBroker
{
    /// <inheritdoc />
    public Task<Result<FileAuditPage>> ListAsync(FileAuditQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var parameters = new List<KeyValuePair<string, string?>> { new("limit", query.Limit.ToString(CultureInfo.InvariantCulture)) };
        if (query.FileId is { } fileId)
            parameters.Add(new("fileId", fileId.ToString()));
        if (query.Action is { } action)
            parameters.Add(new("action", FilesApiRouteMapper.WireName(action)));
        if (query.Cursor is not null)
            parameters.Add(new("cursor", query.Cursor));

        return transport.SendAsync<FileAuditPage>(new HttpRequestMessage(HttpMethod.Get, FilesApiRouteMapper.Audit(parameters)), cancellationToken);
    }
}
