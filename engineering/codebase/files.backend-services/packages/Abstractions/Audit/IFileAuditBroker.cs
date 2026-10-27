using WoW.Two.Sdk.Beta.Be.Foundation.Results;

namespace WoW.Two.Sdk.Beta.Be.Files.Audit;

/// <summary>Integrates the Files audit trail — who uploaded, read, changed, deleted or restored the tenant's files — in
/// process or over the service.</summary>
public interface IFileAuditBroker
{
    /// <summary>Lists the tenant's audit entries, newest first.</summary>
    /// <param name="query">The filters and the page.</param>
    /// <param name="cancellationToken">The token that cancels the call.</param>
    /// <returns>One page of entries.</returns>
    Task<Result<FileAuditPage>> ListAsync(FileAuditQuery query, CancellationToken cancellationToken = default);
}
