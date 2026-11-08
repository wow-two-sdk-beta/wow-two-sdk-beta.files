namespace WoW.Two.Sdk.Beta.Be.Files.Module.Imports;

/// <summary>Represents one moved file: a source key and the file it became.</summary>
internal sealed class FileImportItemEntity
{
    /// <summary>Holds the table this entity maps to.</summary>
    public const string TableName = "files_import_items";

    /// <summary>Gets or sets the insertion order, the map's cursor.</summary>
    public long Seq { get; set; }

    /// <summary>Gets or sets the import.</summary>
    public required string ImportId { get; set; }

    /// <summary>Gets or sets the source.</summary>
    public required string SourceId { get; set; }

    /// <summary>Gets or sets the key in the source; a composite key joins its values with <c>/</c>.</summary>
    public required string SourceKey { get; set; }

    /// <summary>Gets or sets the file the key became.</summary>
    public required string FileId { get; set; }

    /// <summary>Gets or sets the SHA-256 of the bytes, lowercase hex.</summary>
    public required string Sha256 { get; set; }

    /// <summary>Gets or sets the size in bytes.</summary>
    public long Size { get; set; }

    /// <summary>Gets or sets the reference attached to the file, <c>{recordType}:{recordId}</c>.</summary>
    public string? Reference { get; set; }

    /// <summary>Gets or sets when the item was recorded (UTC).</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }
}
