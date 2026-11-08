namespace WoW.Two.Sdk.Beta.Be.Files.Module.Persistence;

/// <summary>Represents one image variant of a stored file; its bytes are a tenant blob like any file's, keyed by their
/// own SHA-256.</summary>
internal sealed class FileVariantEntity
{
    /// <summary>Holds the table this entity maps to.</summary>
    public const string TableName = "files_variants";

    /// <summary>Gets or sets the tenant of the file and its bytes.</summary>
    public required string TenantId { get; set; }

    /// <summary>Gets or sets the source file's id.</summary>
    public required string FileId { get; set; }

    /// <summary>Gets or sets the width in pixels; with the file it keys the variant.</summary>
    public int Width { get; set; }

    /// <summary>Gets or sets the height in pixels.</summary>
    public int Height { get; set; }

    /// <summary>Gets or sets the content type of the variant's bytes.</summary>
    public required string ContentType { get; set; }

    /// <summary>Gets or sets the size in bytes.</summary>
    public long Size { get; set; }

    /// <summary>Gets or sets the SHA-256 of the variant's bytes, lowercase hex; with the tenant it keys the blob.</summary>
    public required string Sha256 { get; set; }

    /// <summary>Gets or sets when the variant was made (UTC).</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }
}
