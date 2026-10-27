namespace WoW.Two.Sdk.Beta.Be.Files.Module.Persistence;

/// <summary>Represents one record's use of a file.</summary>
internal sealed class FileReferenceEntity
{
    /// <summary>Holds the table this entity maps to.</summary>
    public const string TableName = "files_references";

    /// <summary>Gets or sets the tenant of the file and the record.</summary>
    public required string TenantId { get; set; }

    /// <summary>Gets or sets the referenced file's id.</summary>
    public required string FileId { get; set; }

    /// <summary>Gets or sets the record's type, product-qualified.</summary>
    public required string RecordType { get; set; }

    /// <summary>Gets or sets the record's id.</summary>
    public required string RecordId { get; set; }

    /// <summary>Gets or sets when the reference was attached (UTC).</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }
}
