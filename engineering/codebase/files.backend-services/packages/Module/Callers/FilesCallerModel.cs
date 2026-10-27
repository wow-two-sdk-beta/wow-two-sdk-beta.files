namespace WoW.Two.Sdk.Beta.Be.Files.Module.Callers;

/// <summary>Represents who a Files call acts for: the tenant whose files it sees, and the subject, organization and
/// app it stamps on new files.</summary>
public sealed record FilesCallerModel
{
    /// <summary>Gets the tenant; every read and write stays inside it.</summary>
    public required string TenantId { get; init; }

    /// <summary>Gets the person, client or key making the call.</summary>
    public string? SubjectId { get; init; }

    /// <summary>Gets the caller's active organization.</summary>
    public string? OrganizationId { get; init; }

    /// <summary>Gets the calling app, whose policy applies when an upload names none.</summary>
    public string? AppId { get; init; }
}
