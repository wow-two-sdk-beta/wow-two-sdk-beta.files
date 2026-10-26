namespace WoW.Two.Sdk.Beta.Be.Files.Store;

/// <summary>Changes a file's metadata; a null member leaves its field as it is.</summary>
public sealed record FilePatch
{
    /// <summary>Gets the new name.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the new visibility.</summary>
    public FileVisibility? Visibility { get; init; }

    /// <summary>Gets the new expiry (UTC).</summary>
    public DateTimeOffset? ExpiresAtUtc { get; init; }
}
