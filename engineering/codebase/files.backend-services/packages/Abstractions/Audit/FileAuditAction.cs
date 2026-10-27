namespace WoW.Two.Sdk.Beta.Be.Files.Audit;

/// <summary>Refers to what an audited call did to a file.</summary>
public enum FileAuditAction
{
    /// <summary>An upload, accepted or refused.</summary>
    Upload = 0,

    /// <summary>A read of the bytes: an API read, a signed URL or a public URL.</summary>
    Download,

    /// <summary>A rename, a visibility change or a new expiry.</summary>
    Update,

    /// <summary>A soft delete.</summary>
    Delete,

    /// <summary>A restore of a deleted file.</summary>
    Restore,
}
