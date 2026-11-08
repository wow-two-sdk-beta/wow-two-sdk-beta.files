namespace WoW.Two.Sdk.Beta.Be.Files.Module.Imports;

/// <summary>Represents one source of an import, with the pushes it refused.</summary>
internal sealed class FileImportSourceEntity
{
    /// <summary>Holds the table this entity maps to.</summary>
    public const string TableName = "files_import_sources";

    /// <summary>Gets or sets the import.</summary>
    public required string ImportId { get; set; }

    /// <summary>Gets or sets the source's id, such as <c>brand-media</c>.</summary>
    public required string SourceId { get; set; }

    /// <summary>Gets or sets how many pushes into the source were refused.</summary>
    public long Failed { get; set; }
}
