namespace WoW.Two.Sdk.Beta.Be.Files.Module.Imports;

/// <summary>Describes one file pushed into an import.</summary>
public sealed record FileImportPushSpec
{
    /// <summary>Gets the source; one of the import's sources.</summary>
    public required string Source { get; init; }

    /// <summary>Gets the key in the source; a composite key joins its values with <c>/</c>.</summary>
    public required string Key { get; init; }

    /// <summary>Gets the SHA-256 the mover read, lowercase hex; Files checks the bytes against it.</summary>
    public required string Sha256 { get; init; }

    /// <summary>Gets the reference to attach, <c>{recordType}:{recordId}</c>; none attaches nothing.</summary>
    public string? Reference { get; init; }

    /// <summary>Gets the file name; none takes the key's last segment.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the content type; none lets Files detect it from the bytes.</summary>
    public string? ContentType { get; init; }

    /// <summary>Gets the app whose policy applies; none takes the reference's text before its first <c>.</c>.</summary>
    public string? App { get; init; }
}
