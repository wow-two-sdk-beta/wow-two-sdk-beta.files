using WoW.Two.Sdk.Beta.Be.Files.Store;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Policies;

/// <summary>Holds what one app may upload and how long its deleted files stay restorable.</summary>
public sealed class FilePolicyOptions
{
    /// <summary>Gets or sets the largest file accepted, in bytes. Default 100 MiB.</summary>
    public long MaxBytes { get; set; } = 100L * 1024 * 1024;

    /// <summary>Gets the kinds accepted; empty accepts every kind.</summary>
    public HashSet<FileKind> AllowedKinds { get; } = [];

    /// <summary>Gets the extensions accepted, without the dot; empty accepts every extension.</summary>
    public HashSet<string> AllowedExtensions { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets or sets whether programs, scripts and macro documents are accepted. Default off.</summary>
    public bool AllowExecutables { get; set; }

    /// <summary>Gets or sets how long a deleted file stays restorable before the sweep purges it. Default 30 days.</summary>
    public TimeSpan DeletedRetention { get; set; } = TimeSpan.FromDays(30);
}
