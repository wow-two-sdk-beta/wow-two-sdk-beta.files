using System.Security.Cryptography;

namespace WoW.Two.Sdk.Beta.Be.Files.Testing.Files;

/// <summary>Holds small byte samples that carry real magic bytes, for upload tests.</summary>
public static class FilesSamples
{
    /// <summary>Holds bytes that start like a PNG.</summary>
    public static byte[] Png => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x0D, 0x49, 0x48, 0x44, 0x52, 1, 2, 3, 4];

    /// <summary>Holds bytes that start like a JPEG.</summary>
    public static byte[] Jpeg => [0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10, 0x4A, 0x46, 0x49, 0x46, 0, 1, 1, 0, 0, 1, 0, 1, 0, 0];

    /// <summary>Holds a short PDF header.</summary>
    public static byte[] Pdf => "%PDF-1.7\n%âã\n1 0 obj\n<<>>\nendobj\n"u8.ToArray();

    /// <summary>Returns the SHA-256 of bytes, lowercase hex, as Files reports it.</summary>
    /// <param name="bytes">The bytes.</param>
    /// <returns>The digest.</returns>
    public static string Sha256(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
}
