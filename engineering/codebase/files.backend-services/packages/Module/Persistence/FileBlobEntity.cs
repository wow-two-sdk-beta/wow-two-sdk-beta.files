namespace WoW.Two.Sdk.Beta.Be.Files.Module.Persistence;

/// <summary>Represents one tenant's copy of some bytes, shared by every file with the same SHA-256.</summary>
internal sealed class FileBlobEntity
{
    /// <summary>Holds the table this entity maps to.</summary>
    public const string TableName = "files_blobs";

    /// <summary>Gets or sets the owning tenant.</summary>
    public required string TenantId { get; set; }

    /// <summary>Gets or sets the SHA-256 of the bytes, lowercase hex.</summary>
    public required string Sha256 { get; set; }

    /// <summary>Gets or sets the key in the blob store.</summary>
    public required string StorageKey { get; set; }

    /// <summary>Gets or sets the size in bytes.</summary>
    public long Size { get; set; }

    /// <summary>Gets or sets when the bytes were first stored (UTC).</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>Gets or sets when an upload last stored or reused the bytes (UTC); the sweep spares recent ones.</summary>
    public DateTimeOffset LastUsedAtUtc { get; set; }
}
