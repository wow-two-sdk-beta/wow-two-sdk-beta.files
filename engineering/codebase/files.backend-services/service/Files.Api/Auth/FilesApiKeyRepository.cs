using Microsoft.Extensions.Options;
using WoW.Two.Sdk.Beta.Be.Identity.ApiKeys;

namespace Files.Api.Auth;

/// <summary>Finds a tenant API key among the SHA-256 hashes in <c>Files:Auth:ApiKeys</c> (t8 C9). One lookup answers the
/// key's tenant, so a key carries no tenant prefix.</summary>
/// <remarks>P2 reads a keys table with a unique hash index and records use; configured keys record none.</remarks>
/// <param name="settings">The API's caller settings.</param>
internal sealed class FilesApiKeyRepository(IOptionsMonitor<FilesAuthSettings> settings) : IApiKeyRepository
{
    /// <inheritdoc />
    public Task<ApiKeyRecord?> FindLiveByHashAsync(string hash, CancellationToken cancellationToken)
    {
        var key = settings.CurrentValue.ApiKeys.FirstOrDefault(candidate =>
            candidate.Sha256.Length == 64 && string.Equals(candidate.Sha256, hash, StringComparison.OrdinalIgnoreCase));

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
}
