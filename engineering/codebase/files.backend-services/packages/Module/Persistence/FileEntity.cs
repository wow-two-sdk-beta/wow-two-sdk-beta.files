namespace WoW.Two.Sdk.Beta.Be.Files.Module.Persistence;

/// <summary>Represents one stored file — the row an upload creates.</summary>
internal sealed class FileEntity
{
    /// <summary>Holds the table this entity maps to.</summary>
    public const string TableName = "files_files";

    /// <summary>Gets or sets the insertion order, the paging key.</summary>
    public long Seq { get; set; }

    /// <summary>Gets or sets the public file id, a lowercase UUIDv7.</summary>
    public required string Id { get; set; }

    /// <summary>Gets or sets the owning tenant.</summary>
    public required string TenantId { get; set; }

    /// <summary>Gets or sets the uploader's organization.</summary>
    public string? OrganizationId { get; set; }

    /// <summary>Gets or sets the app whose policy governs the file.</summary>
    public string? AppId { get; set; }

    /// <summary>Gets or sets the uploading subject.</summary>
    public string? OwnerId { get; set; }

    /// <summary>Gets or sets the safe name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the registry content type.</summary>
    public required string ContentType { get; set; }

    /// <summary>Gets or sets the <c>FileKind</c> name.</summary>
    public required string Kind { get; set; }

    /// <summary>Gets or sets the size in bytes.</summary>
    public long Size { get; set; }

    /// <summary>Gets or sets the SHA-256 of the bytes, lowercase hex; with the tenant it keys the blob.</summary>
    public required string Sha256 { get; set; }

    /// <summary>Gets or sets the <c>FileVisibility</c> name.</summary>
    public required string Visibility { get; set; }

    /// <summary>Gets or sets the <c>FileState</c> name.</summary>
    public required string State { get; set; }

    /// <summary>Gets or sets the key that makes a retried upload return this file.</summary>
    public string? IdempotencyKey { get; set; }

    /// <summary>Gets or sets when the file was created (UTC).</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>Gets or sets when the file expires (UTC).</summary>
    public DateTimeOffset? ExpiresAtUtc { get; set; }

    /// <summary>Gets or sets when the file was deleted (UTC).</summary>
    public DateTimeOffset? DeletedAtUtc { get; set; }

    /// <summary>Gets or sets when the sweep purges the deleted file (UTC).</summary>
    public DateTimeOffset? PurgeAtUtc { get; set; }
}
