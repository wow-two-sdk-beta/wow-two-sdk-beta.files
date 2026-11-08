using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Files.Urls;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Urls;

/// <summary>Issues and checks the content host's URLs: signed ones for private files, HMAC-SHA256 over the tenant, file,
/// expiry, disposition and, for a variant, its width; content-addressed ones for public files and variants.</summary>
internal sealed class SignedFileUrlIssuer(IOptions<FilesModuleOptions> options, TimeProvider time)
{
    /// <summary>Holds the query parameter of the expiry, in Unix seconds.</summary>
    public const string ExpiresParameter = "exp";

    /// <summary>Holds the query parameter of the disposition: <c>i</c> inline, <c>a</c> attachment.</summary>
    public const string DispositionParameter = "d";

    /// <summary>Holds the query parameter of the signature.</summary>
    public const string SignatureParameter = "sig";

    /// <summary>Holds the query parameter of a variant's width; absent for the original.</summary>
    public const string WidthParameter = "w";

    /// <summary>Issues a signed URL to a file, or to one of its variants.</summary>
    /// <param name="tenantId">The file's tenant.</param>
    /// <param name="id">The file.</param>
    /// <param name="lifetime">How long the URL stays valid; capped at the delivery's maximum.</param>
    /// <param name="disposition">Whether the browser shows or saves the file.</param>
    /// <param name="width">The variant's width; none for the original.</param>
    public SignedFileUrl Issue(string tenantId, FileId id, TimeSpan lifetime, FileDisposition disposition, int? width = null)
    {
        var delivery = options.Value.Delivery;
        var key = Key(delivery);
        var capped = lifetime <= TimeSpan.Zero ? TimeSpan.FromMinutes(5) : lifetime < delivery.MaxLifetime ? lifetime : delivery.MaxLifetime;
        var expires = (time.GetUtcNow() + capped).ToUnixTimeSeconds();
        var flag = disposition == FileDisposition.Attachment ? "a" : "i";
        var variant = width is { } w ? string.Create(CultureInfo.InvariantCulture, $"&{WidthParameter}={w}") : string.Empty;
        var query = string.Create(CultureInfo.InvariantCulture,
            $"{ExpiresParameter}={expires}&{DispositionParameter}={flag}{variant}&{SignatureParameter}={Sign(key, tenantId, id, expires, flag, width)}");

        return new SignedFileUrl(Build(tenantId, $"{FilesRouteConstants.SignedFile}/{id}?{query}"), DateTimeOffset.FromUnixTimeSeconds(expires));
    }

    /// <summary>Builds the immutable URL of a public file's bytes, or of a variant's.</summary>
    public Uri PublicUrl(string tenantId, string sha256, string extension) =>
        Build(tenantId, $"{FilesRouteConstants.PublicFile}/{sha256}.{extension}");

    /// <summary>Checks a signed URL's parts: the signature matches and the expiry has not passed.</summary>
    /// <param name="tenantId">The tenant in the URL's path.</param>
    /// <param name="id">The file in the URL's path.</param>
    /// <param name="expires">The expiry parameter.</param>
    /// <param name="disposition">The disposition parameter.</param>
    /// <param name="signature">The signature parameter.</param>
    /// <param name="width">The width parameter; none for the original.</param>
    public bool Verify(string tenantId, FileId id, long expires, string disposition, string? signature, int? width = null)
    {
        if (signature is null || time.GetUtcNow().ToUnixTimeSeconds() > expires)
            return false;

        var expected = Encoding.ASCII.GetBytes(Sign(Key(options.Value.Delivery), tenantId, id, expires, disposition, width));
        return CryptographicOperations.FixedTimeEquals(expected, Encoding.ASCII.GetBytes(signature));
    }

    private Uri Build(string tenantId, string path)
    {
        var baseUrl = options.Value.Delivery.BaseUrl
            ?? throw new InvalidOperationException("Set Files:Delivery:BaseUrl to issue file URLs.");
        var prefix = tenantId == options.Value.DefaultTenantId ? string.Empty : $"t/{tenantId}/";
        return new Uri($"{baseUrl.ToString().TrimEnd('/')}/{prefix}{path}");
    }

    private static byte[] Key(FileDeliveryOptions delivery) =>
        delivery.SigningKey.Length >= 32
            ? Encoding.UTF8.GetBytes(delivery.SigningKey)
            : throw new InvalidOperationException("Set Files:Delivery:SigningKey (32+ characters) to sign file URLs.");

    private static string Sign(byte[] key, string tenantId, FileId id, long expires, string disposition, int? width)
    {
        // The original's payload is unchanged from before variants, so URLs issued then still verify.
        var payload = width is { } w
            ? string.Create(CultureInfo.InvariantCulture, $"{tenantId}\n{id}\n{expires}\n{disposition}\n{w}")
            : string.Create(CultureInfo.InvariantCulture, $"{tenantId}\n{id}\n{expires}\n{disposition}");
        return Convert.ToBase64String(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(payload)))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
