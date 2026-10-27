using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Files.Urls;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Urls;

/// <summary>Issues and checks the content host's URLs: signed ones for private files, HMAC-SHA256 over the tenant, file,
/// expiry and disposition; content-addressed ones for public files.</summary>
internal sealed class SignedFileUrlIssuer(IOptions<FilesModuleOptions> options, TimeProvider time)
{
    /// <summary>Holds the query parameter of the expiry, in Unix seconds.</summary>
    public const string ExpiresParameter = "exp";

    /// <summary>Holds the query parameter of the disposition: <c>i</c> inline, <c>a</c> attachment.</summary>
    public const string DispositionParameter = "d";

    /// <summary>Holds the query parameter of the signature.</summary>
    public const string SignatureParameter = "sig";

    /// <summary>Issues a signed URL to a file.</summary>
    public SignedFileUrl Issue(string tenantId, FileId id, TimeSpan lifetime, FileDisposition disposition)
    {
        var delivery = options.Value.Delivery;
        var key = Key(delivery);
        var capped = lifetime <= TimeSpan.Zero ? TimeSpan.FromMinutes(5) : lifetime < delivery.MaxLifetime ? lifetime : delivery.MaxLifetime;
        var expires = (time.GetUtcNow() + capped).ToUnixTimeSeconds();
        var flag = disposition == FileDisposition.Attachment ? "a" : "i";
        var query = string.Create(CultureInfo.InvariantCulture,
            $"{ExpiresParameter}={expires}&{DispositionParameter}={flag}&{SignatureParameter}={Sign(key, tenantId, id, expires, flag)}");

        return new SignedFileUrl(Build(tenantId, $"{FilesRouteConstants.SignedFile}/{id}?{query}"), DateTimeOffset.FromUnixTimeSeconds(expires));
    }

    /// <summary>Builds the immutable URL of a public file's bytes.</summary>
    public Uri PublicUrl(string tenantId, string sha256, string extension) =>
        Build(tenantId, $"{FilesRouteConstants.PublicFile}/{sha256}.{extension}");

    /// <summary>Checks a signed URL's parts: the signature matches and the expiry has not passed.</summary>
    public bool Verify(string tenantId, FileId id, long expires, string disposition, string? signature)
    {
        if (signature is null || time.GetUtcNow().ToUnixTimeSeconds() > expires)
            return false;

        var expected = Encoding.ASCII.GetBytes(Sign(Key(options.Value.Delivery), tenantId, id, expires, disposition));
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

    private static string Sign(byte[] key, string tenantId, FileId id, long expires, string disposition)
    {
        var payload = string.Create(CultureInfo.InvariantCulture, $"{tenantId}\n{id}\n{expires}\n{disposition}");
        return Convert.ToBase64String(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(payload)))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
