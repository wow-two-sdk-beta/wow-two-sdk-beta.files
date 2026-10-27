using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WoW.Two.Sdk.Beta.Be.Files.Module.Delivery;
using WoW.Two.Sdk.Beta.Be.Web.SecureHeaders;

namespace WoW.Two.Sdk.Beta.Be.Files.Server.Delivery;

/// <summary>Maps the content host's routes over the Files module.</summary>
public static class FilesDeliveryEndpointRouteBuilderExtensions
{
    private static readonly string[] Methods = [HttpMethods.Get, HttpMethods.Head];

    /// <summary>Maps <c>GET</c> and <c>HEAD</c> on <c>/f/{id}</c> (signed, private) and <c>/m/{sha256}.{ext}</c>
    /// (public, immutable), each also under <c>/t/{tenant}/</c> for a tenant other than the default.</summary>
    /// <remarks>Map them on the content host (<c>files.{domain}</c> or the shared host), never on an app's origin. The
    /// endpoints carry <see cref="FilesDeliveryEndpointMetadata"/>, allow cross-origin embedding and override the be sdk
    /// preset's content policy, embedder policy and referrer policy.</remarks>
    /// <param name="endpoints">The route builder.</param>
    /// <returns>The group of delivery routes, for further conventions.</returns>
    public static RouteGroupBuilder MapFilesDelivery(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        // The be sdk preset writes the API's headers as the response starts; these routes answer user content's own.
        var group = endpoints.MapGroup(string.Empty)
            .WithMetadata(new FilesDeliveryEndpointMetadata())
            .AllowCrossOriginEmbedding()
            .OverrideSecureHeader("Content-Security-Policy", FilesDeliveryHeaders.ContentSecurityPolicy)
            .OverrideSecureHeader("Cross-Origin-Embedder-Policy", null)
            .OverrideSecureHeader("Referrer-Policy", "no-referrer")
            .ExcludeFromDescription();

        group.MapMethods($"/{FilesRouteConstants.SignedFile}/{{id}}", Methods,
            (HttpContext http, string id, IFilesDeliveryService delivery, TimeProvider time) =>
                FilesDeliveryHandlers.ServeSignedAsync(http, id, null, delivery, time));
        group.MapMethods($"/t/{{tenant}}/{FilesRouteConstants.SignedFile}/{{id}}", Methods,
            (HttpContext http, string tenant, string id, IFilesDeliveryService delivery, TimeProvider time) =>
                FilesDeliveryHandlers.ServeSignedAsync(http, id, tenant, delivery, time));
        group.MapMethods($"/{FilesRouteConstants.PublicFile}/{{file}}", Methods,
            (HttpContext http, string file, IFilesDeliveryService delivery) =>
                FilesDeliveryHandlers.ServePublicAsync(http, file, null, delivery));
        group.MapMethods($"/t/{{tenant}}/{FilesRouteConstants.PublicFile}/{{file}}", Methods,
            (HttpContext http, string tenant, string file, IFilesDeliveryService delivery) =>
                FilesDeliveryHandlers.ServePublicAsync(http, file, tenant, delivery));

        return group;
    }
}
