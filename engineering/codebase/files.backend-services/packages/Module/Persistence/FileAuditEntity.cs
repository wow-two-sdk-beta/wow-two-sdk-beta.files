namespace WoW.Two.Sdk.Beta.Be.Files.Module.Persistence;

/// <summary>Represents one audited call on a tenant's file.</summary>
internal sealed class FileAuditEntity
{
    /// <summary>Holds the table this entity maps to.</summary>
    public const string TableName = "files_audit";

    /// <summary>Gets or sets the entry's order, the paging key.</summary>
    public long Seq { get; set; }

    /// <summary>Gets or sets the tenant.</summary>
    public required string TenantId { get; set; }

    /// <summary>Gets or sets the file's id; none for an upload refused before a file existed.</summary>
    public string? FileId { get; set; }

    /// <summary>Gets or sets the <c>FileAuditAction</c> name.</summary>
    public required string Action { get; set; }

    /// <summary>Gets or sets the <c>FileAuditOutcome</c> name.</summary>
    public required string Outcome { get; set; }

    /// <summary>Gets or sets the refusal's <c>AppErrorType</c> name.</summary>
    public string? ErrorCode { get; set; }

    /// <summary>Gets or sets the calling subject.</summary>
    public string? SubjectId { get; set; }

    /// <summary>Gets or sets the calling app.</summary>
    public string? AppId { get; set; }

    /// <summary>Gets or sets the bytes received or served.</summary>
    public long? Bytes { get; set; }

    /// <summary>Gets or sets when the call happened (UTC).</summary>
    public DateTimeOffset OccurredAtUtc { get; set; }
}
