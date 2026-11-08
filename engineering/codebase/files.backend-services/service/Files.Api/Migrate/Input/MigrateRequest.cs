namespace Files.Api.Migrate.Input;

/// <summary>Represents the mover's standard input: where Files runs, the import, its token and the one source to move
/// (<c>product/features/imports.md</c> § Mover).</summary>
/// <remarks><see cref="Files"/>, <see cref="Import"/> and <see cref="Token"/> are required by <c>push</c> and
/// <c>verify</c> only; <c>plan</c> touches nothing remote.</remarks>
public sealed record MigrateRequest
{
    /// <summary>Gets the Files service's base address, ending in <c>/</c>.</summary>
    public Uri? Files { get; init; }

    /// <summary>Gets the import's id.</summary>
    public string? Import { get; init; }

    /// <summary>Gets the import token, sent as <c>X-Import-Token</c>.</summary>
    public string? Token { get; init; }

    /// <summary>Gets the source this run moves.</summary>
    public required MigrateSource Source { get; init; }

    /// <summary>Gets the read-only PostgreSQL connection string; <c>postgres-column</c> only.</summary>
    public string? Connection { get; init; }

    /// <summary>Gets the folder a <c>volume-folder</c> path or a <c>sqlite-column</c> database sits under.</summary>
    public string? Root { get; init; }
}
