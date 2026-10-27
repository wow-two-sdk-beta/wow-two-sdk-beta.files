using Microsoft.AspNetCore.Http;

namespace WoW.Two.Sdk.Beta.Be.Files.Server.Delivery;

/// <summary>Holds the headers every content-host response carries, so user content never runs as active content on
/// the host and other origins may still embed it.</summary>
internal static class FilesDeliveryHeaders
{
    /// <summary>Holds the policy of served content: no script, no plugin, no form, sandboxed.</summary>
    public const string ContentSecurityPolicy = "default-src 'none'; style-src 'unsafe-inline'; sandbox";

    /// <summary>Writes the headers, replacing what a host preset wrote.</summary>
    public static void Apply(HttpResponse response)
    {
        response.Headers.XContentTypeOptions = "nosniff";
        response.Headers.ContentSecurityPolicy = ContentSecurityPolicy;
        response.Headers["Cross-Origin-Resource-Policy"] = "cross-origin";
        response.Headers.Remove("Cross-Origin-Embedder-Policy");
        response.Headers["Referrer-Policy"] = "no-referrer";
    }
}
