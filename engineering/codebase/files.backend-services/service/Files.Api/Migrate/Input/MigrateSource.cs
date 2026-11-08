namespace Files.Api.Migrate.Input;

/// <summary>Represents one manifest source as the mover reads it; fields it does not use (<c>wave</c>, <c>volume</c>)
/// are ignored.</summary>
public sealed record MigrateSource
{
    /// <summary>Gets the source id, one of the import's sources.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the source kind, one of <see cref="MigrateSourceKindConstants"/>.</summary>
    public required string Kind { get; init; }

    /// <summary>Gets the folder below the root whose files a <c>volume-folder</c> source reads.</summary>
    public string? Path { get; init; }

    /// <summary>Gets the SQLite file below the root a <c>sqlite-column</c> source opens.</summary>
    public string? Database { get; init; }

    /// <summary>Gets the table a column source reads; a <c>schema.table</c> name is quoted part by part.</summary>
    public string? Table { get; init; }

    /// <summary>Gets the key columns of a column source, in order; a composite key joins their values with <c>/</c>.</summary>
    public IReadOnlyList<string> Key { get; init; } = [];

    /// <summary>Gets the column that holds a column source's bytes.</summary>
    public string? Content { get; init; }

    /// <summary>Gets the optional column that holds a file's name.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the optional column that holds a file's content type; without it Files detects the type.</summary>
    public string? Type { get; init; }

    /// <summary>Gets the optional reference template attached to each file.</summary>
    public ReferenceTemplate? Reference { get; init; }
}
