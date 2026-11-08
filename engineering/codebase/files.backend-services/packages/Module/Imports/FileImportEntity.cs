namespace WoW.Two.Sdk.Beta.Be.Files.Module.Imports;

/// <summary>Represents one import: a product's legacy files moving into a tenant.</summary>
internal sealed class FileImportEntity
{
    /// <summary>Holds the table this entity maps to.</summary>
    public const string TableName = "files_imports";

    /// <summary>Gets or sets the import's id, a lowercase UUIDv7.</summary>
    public required string Id { get; set; }

    /// <summary>Gets or sets the tenant the import's files land in.</summary>
    public required string TenantId { get; set; }

    /// <summary>Gets or sets the state; <see cref="FileImportConstants.OpenState"/> in P1.</summary>
    public required string State { get; set; }

    /// <summary>Gets or sets when the import was opened (UTC).</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }
}
