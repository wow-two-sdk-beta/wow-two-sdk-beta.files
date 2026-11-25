using WoW.Two.Sdk.Beta.Be.Identity.CurrentPrincipal;

namespace Files.Application.Access.Models;

/// <summary>Assigns a file operation to its authorized tenant, principal and acting app.</summary>
public sealed record FileRequestActor(string TenantId, PrincipalKind Kind, string? SubjectId, string? Issuer, string? OrganizationId, string? AppId, string? ActorId);
