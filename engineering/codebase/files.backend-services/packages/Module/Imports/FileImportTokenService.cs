using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Imports;

/// <summary>Issues and reads import tokens: <c>fimp_{payload}.{mac}</c>, the payload base64url JSON
/// <c>{ "imp": id, "ten": tenant, "exp": unix seconds }</c>, the mac base64url HMAC-SHA256 over the payload text under a key
/// derived from <c>Files:Delivery:SigningKey</c> (HKDF-SHA256, info <see cref="FileImportConstants.TokenKeyInfo"/>).</summary>
/// <param name="options">The module settings, for the signing key.</param>
/// <param name="imports">The import settings, for the lifetime.</param>
/// <param name="time">The clock.</param>
internal sealed class FileImportTokenService(IOptions<FilesModuleOptions> options, IOptions<FileImportOptions> imports, TimeProvider time) : IFileImportTokenService
{
    /// <summary>Holds the longest token read; anything longer is not one of ours.</summary>
    private const int MaxTokenLength = 1024;

    /// <inheritdoc />
    public FileImportTokenModel Issue(string importId, string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(importId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        var key = Key() ?? throw new InvalidOperationException("Set Files:Delivery:SigningKey (32+ characters) to issue import tokens.");
        var lifetime = imports.Value.TokenLifetime > TimeSpan.Zero ? imports.Value.TokenLifetime : new FileImportOptions().TokenLifetime;
        var expires = (time.GetUtcNow() + lifetime).ToUnixTimeSeconds();

        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("imp", importId);
            writer.WriteString("ten", tenantId);
            writer.WriteNumber("exp", expires);
            writer.WriteEndObject();
        }

        var payload = Base64Url.EncodeToString(buffer.ToArray());
        return new FileImportTokenModel($"{FileImportConstants.TokenMarker}{payload}.{Mac(key, payload)}", DateTimeOffset.FromUnixTimeSeconds(expires));
    }

    /// <inheritdoc />
    public FileImportTokenClaims? Read(string? token)
    {
        if (token is null || token.Length > MaxTokenLength || !token.StartsWith(FileImportConstants.TokenMarker, StringComparison.Ordinal))
            return null;

        var body = token.AsSpan(FileImportConstants.TokenMarker.Length);
        var dot = body.IndexOf('.');
        if (dot <= 0 || Key() is not { } key)
            return null;

        var payload = body[..dot].ToString();
        var expected = Encoding.ASCII.GetBytes(Mac(key, payload));
        if (!CryptographicOperations.FixedTimeEquals(expected, Encoding.ASCII.GetBytes(body[(dot + 1)..].ToString())))
            return null;

        try
        {
            using var document = JsonDocument.Parse(Base64Url.DecodeFromChars(payload));
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("imp", out var imp) || imp.ValueKind != JsonValueKind.String || imp.GetString() is not { Length: > 0 } importId
                || !root.TryGetProperty("ten", out var ten) || ten.ValueKind != JsonValueKind.String || ten.GetString() is not { Length: > 0 } tenantId
                || !root.TryGetProperty("exp", out var exp) || !exp.TryGetInt64(out var expires))
                return null;

            return time.GetUtcNow().ToUnixTimeSeconds() >= expires
                ? null
                : new FileImportTokenClaims(importId, tenantId, DateTimeOffset.FromUnixTimeSeconds(expires));
        }
        catch (Exception exception) when (exception is FormatException or JsonException or ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    /// <summary>Derives the token key from the signing key; none while the signing key is shorter than 32 characters.</summary>
    private byte[]? Key()
    {
        var signingKey = options.Value.Delivery.SigningKey;
        return signingKey.Length >= 32
            ? HKDF.DeriveKey(HashAlgorithmName.SHA256, Encoding.UTF8.GetBytes(signingKey), 32, salt: [], info: Encoding.UTF8.GetBytes(FileImportConstants.TokenKeyInfo))
            : null;
    }

    private static string Mac(byte[] key, string payload) =>
        Base64Url.EncodeToString(HMACSHA256.HashData(key, Encoding.ASCII.GetBytes(payload)));
}
