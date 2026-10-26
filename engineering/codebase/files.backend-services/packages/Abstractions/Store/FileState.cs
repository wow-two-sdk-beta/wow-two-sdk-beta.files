namespace WoW.Two.Sdk.Beta.Be.Files.Store;

/// <summary>Tracks a file from upload to use: <c>Uploading</c> → <c>Pending</c> → <c>Ready</c>, or a terminal refusal.</summary>
public enum FileState
{
    /// <summary>Bytes are still arriving.</summary>
    Uploading = 0,

    /// <summary>Bytes are stored; checks, scan or presets are running.</summary>
    Pending,

    /// <summary>The file passed its checks and can be served.</summary>
    Ready,

    /// <summary>The file failed validation and is never served.</summary>
    Rejected,

    /// <summary>The scanner flagged the file; only an admin reaches it.</summary>
    Quarantined,
}
