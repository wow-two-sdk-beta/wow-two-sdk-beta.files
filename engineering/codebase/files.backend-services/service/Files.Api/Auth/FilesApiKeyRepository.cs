using Microsoft.Extensions.Options;
using WoW.Two.Sdk.Beta.Be.Files;
using WoW.Two.Sdk.Beta.Be.Identity.ApiKeys;

namespace Files.Api.Auth;

/// <summary>Finds an API key: the operator key of <c>Files:Operator:ApiKey</c>, else a tenant key among the SHA-256 hashes
/// in <c>Files:Auth:ApiKeys</c> (t8 C9). One lookup answers the key's tenant, so a key carries no tenant prefix.</summary>
/// <remarks>P2 reads a keys table with a unique hash index and records use; configured keys record none. The operator
/// key carries <c>files.tenants</c> alone and no tenant; a configured key never takes its id.</remarks>
/// <param name="settings">The API's caller settings.</param>
/// <param name="operatorSettings">The operator's key.</param>
internal sealed class FilesApiKeyRepository(IOptionsMonitor<FilesAuthSettings> settings, IOptionsMonitor<FilesOperatorSettings> operatorSettings) : IApiKeyRepository
{
    /// <inheritdoc />
    public Task<ApiKeyRecord?> FindLiveByHashAsync(string hash, CancellationToken cancellationToken)
    {
        if (OperatorHash(operatorSettings.CurrentValue) is { } operatorHash && string.Equals(operatorHash, hash, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<ApiKeyRecord?>(new ApiKeyRecord
            {
                Id = FilesAuthConstants.OperatorKeyId,
                Name = FilesAuthConstants.OperatorKeyId,
                Scopes = [FilesScopeConstants.Tenants],
                Partition = null,
            });
        }

        var key = settings.CurrentValue.ApiKeys.FirstOrDefault(candidate =>
            candidate.Sha256.Length == 64 && string.Equals(candidate.Sha256, hash, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(candidate.Id, FilesAuthConstants.OperatorKeyId, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(key is null ? null : new ApiKeyRecord
        {
            Id = key.Id,
            Name = key.Id,
            Scopes = key.Scopes,
            ClientId = key.AppId,
            OrganizationId = key.OrganizationId,
            Partition = key.TenantId,
            ExpiresAt = key.ExpiresAt,
        });
    }

    /// <inheritdoc />
    public Task TouchAsync(string id, DateTimeOffset usedAt, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>Hashes the operator key as presented, <c>files_{value}</c>; none while the setting is empty.</summary>
    internal static string? OperatorHash(FilesOperatorSettings settings) =>
        settings.ApiKey?.Trim() is { Length: > 0 } value
            ? ApiKeySecretFactory.ToHash(value.StartsWith(FilesAuthConstants.ApiKeyMarker, StringComparison.Ordinal) ? value : FilesAuthConstants.ApiKeyMarker + value)
            : null;
}
