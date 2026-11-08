using WoW.Two.Sdk.Beta.Be.Identity.CurrentPrincipal;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Callers;

/// <summary>Represents who a Files call acts for: the tenant whose files it sees, and the subject, issuer, organization,
/// app and actor it stamps on new files and audit rows.</summary>
public sealed record FilesCallerModel
{
    /// <summary>Gets the tenant; every read and write stays inside it.</summary>
    public required string TenantId { get; init; }

    /// <summary>Gets how the caller is identified: a person, a service, an API key, or none.</summary>
    public PrincipalKind Kind { get; init; } = PrincipalKind.Anonymous;

    /// <summary>Gets the person, client or key making the call.</summary>
    public string? SubjectId { get; init; }

    /// <summary>Gets the issuer of the subject; with <see cref="SubjectId"/> it names one person across issuers. Null for
    /// this instance's own API keys and in-process calls.</summary>
    public string? Issuer { get; init; }

    /// <summary>Gets the caller's active organization.</summary>
    public string? OrganizationId { get; init; }

    /// <summary>Gets the calling app, whose policy applies when an upload names none.</summary>
    public string? AppId { get; init; }

    /// <summary>Gets the app acting for the person under token exchange (<c>act.sub</c>); null when the person acts directly.</summary>
    public string? ActorId { get; init; }
}
