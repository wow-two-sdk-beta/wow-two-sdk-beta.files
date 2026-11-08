namespace WoW.Two.Sdk.Beta.Be.Files.Module.Imports;

/// <summary>Describes an import to open.</summary>
public sealed record FileImportSpec
{
    /// <summary>Gets the tenant the files land in; matches <see cref="FilesConstants.TenantIdPattern"/>.</summary>
    public required string Tenant { get; init; }

    /// <summary>Gets the sources the files come from: 1 to <see cref="FileImportConstants.MaxSources"/> distinct ids.</summary>
    public required IReadOnlyList<string> Sources { get; init; }
}
