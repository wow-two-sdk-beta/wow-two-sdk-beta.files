namespace WoW.Two.Sdk.Beta.Be.Files.Store;

/// <summary>Represents one stored file's metadata.</summary>
public sealed record FileModel
{
    /// <summary>Gets the file's id.</summary>
    public required FileId Id { get; init; }

    /// <summary>Gets the tenant that owns the file.</summary>
    public required string TenantId { get; init; }

    /// <summary>Gets the organization the file belongs to, when the caller had one.</summary>
    public string? OrganizationId { get; init; }

    /// <summary>Gets the app that uploaded the file, whose policy governs it.</summary>
    public string? AppId { get; init; }

    /// <summary>Gets the subject that uploaded the file.</summary>
    public string? OwnerId { get; init; }

    /// <summary>Gets the file's display and download name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the content type the bytes were recognized as.</summary>
    public required string ContentType { get; init; }

    /// <summary>Gets the file's kind, from the file-type registry.</summary>
    public required FileKind Kind { get; init; }

    /// <summary>Gets the size in bytes.</summary>
    public required long Size { get; init; }

    /// <summary>Gets the SHA-256 of the bytes, lowercase hex; it keys the stored blob.</summary>
    public required string Sha256 { get; init; }

    /// <summary>Gets who may read the file.</summary>
    public required FileVisibility Visibility { get; init; }

    /// <summary>Gets where the file is between upload and use.</summary>
    public required FileState State { get; init; }

    /// <summary>Gets when the file was created (UTC).</summary>
    public required DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>Gets when the file expires and becomes eligible for purge (UTC), when a lifetime was set.</summary>
    public DateTimeOffset? ExpiresAtUtc { get; init; }

    /// <summary>Gets when the file was deleted (UTC); a deleted file is restorable until its retention ends.</summary>
    public DateTimeOffset? DeletedAtUtc { get; init; }
}
