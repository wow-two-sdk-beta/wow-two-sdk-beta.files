using WoW.Two.Sdk.Beta.Be.Files.Store;

namespace WoW.Two.Sdk.Beta.Be.Files.Audit;

/// <summary>Represents one audited call on a tenant's file.</summary>
public sealed record FileAuditEntryModel
{
    /// <summary>Gets the entry's position in the tenant's audit trail; later entries have larger ones.</summary>
    public required long Sequence { get; init; }

    /// <summary>Gets the file; none for an upload refused before a file existed.</summary>
    public FileId? FileId { get; init; }

    /// <summary>Gets what the call did.</summary>
    public required FileAuditAction Action { get; init; }

    /// <summary>Gets how the call ended.</summary>
    public required FileAuditOutcome Outcome { get; init; }

    /// <summary>Gets the refusal's error type, such as <c>Validation</c>, for a refused call.</summary>
    public string? ErrorCode { get; init; }

    /// <summary>Gets the person, client or key that made the call; none for a URL redemption.</summary>
    public string? SubjectId { get; init; }

    /// <summary>Gets the app the call came from.</summary>
    public string? AppId { get; init; }

    /// <summary>Gets the bytes received or served.</summary>
    public long? Bytes { get; init; }

    /// <summary>Gets when the call happened (UTC).</summary>
    public required DateTimeOffset OccurredAtUtc { get; init; }
}
