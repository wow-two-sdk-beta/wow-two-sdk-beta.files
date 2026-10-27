using System.Text.RegularExpressions;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Blobs;

/// <summary>Maps a tenant and a SHA-256 to the blob store key of their bytes: <c>{tenant}/{sha256[..2]}/{sha256}</c>.</summary>
internal static partial class FileBlobKeyMapper
{
    /// <summary>Maps a tenant and a digest to the storage key.</summary>
    public static string ToKey(string tenantId, string sha256) => $"{tenantId}/{sha256[..2]}/{sha256}";

    /// <summary>Tells whether a tenant id is a safe key segment (<see cref="FilesConstants.TenantIdPattern"/>).</summary>
    public static bool IsValidTenant(string? tenantId) => tenantId is not null && TenantId().IsMatch(tenantId);

    [GeneratedRegex(FilesConstants.TenantIdPattern, RegexOptions.CultureInvariant)]
    private static partial Regex TenantId();
}
