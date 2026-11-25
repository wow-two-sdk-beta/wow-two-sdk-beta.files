using Files.Application.Access.Models;
using WoW.Two.Sdk.Beta.Be.Files.Module.Callers;

namespace Files.Api.Auth;

internal static class FilesCallerExtensions
{
    internal static FileRequestActor ToActor(this IFilesCallerService caller)
    {
        var identity = caller.Current;

        return new FileRequestActor(identity.TenantId, identity.Kind, identity.SubjectId, identity.Issuer,
            identity.OrganizationId, identity.AppId, identity.ActorId);
    }
}
