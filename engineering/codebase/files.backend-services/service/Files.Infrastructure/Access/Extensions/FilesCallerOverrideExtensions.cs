using Files.Application.Access.Models;
using WoW.Two.Sdk.Beta.Be.Files.Module.Callers;

namespace Files.Infrastructure.Access.Extensions;

internal static class FilesCallerOverrideExtensions
{
    internal static IDisposable Use(this FilesCallerOverride overrides, FileRequestActor actor) =>
        overrides.Use(new FilesCallerModel
        {
            TenantId = actor.TenantId,
            Kind = actor.Kind,
            SubjectId = actor.SubjectId,
            Issuer = actor.Issuer,
            OrganizationId = actor.OrganizationId,
            AppId = actor.AppId,
            ActorId = actor.ActorId,
        });
}
