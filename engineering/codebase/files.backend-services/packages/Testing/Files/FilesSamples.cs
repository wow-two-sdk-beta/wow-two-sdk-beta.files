using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace WoW.Two.Sdk.Beta.Be.Files.Testing.Files;

/// <summary>Holds small byte samples that carry real magic bytes, for upload tests, and real images for variant tests.</summary>
public static class FilesSamples
{
    private static readonly uint[] CrcTable = Enumerable.Range(0, 256).Select(index =>
    {
        var value = (uint)index;
        for (var bit = 0; bit < 8; bit++)
            value = (value & 1) != 0 ? 0xEDB88320u ^ (value >> 1) : value >> 1;
        return value;
    }).ToArray();

    /// <summary>Holds bytes that start like a PNG.</summary>
    public static byte[] Png => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x0D, 0x49, 0x48, 0x44, 0x52, 1, 2, 3, 4];

    /// <summary>Holds bytes that start like a JPEG.</summary>
    public static byte[] Jpeg => [0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10, 0x4A, 0x46, 0x49, 0x46, 0, 1, 1, 0, 0, 1, 0, 1, 0, 0];

    /// <summary>Holds a short PDF header.</summary>
    public static byte[] Pdf => "%PDF-1.7\n%âã\n1 0 obj\n<<>>\nendobj\n"u8.ToArray();

    /// <summary>Returns a real, decodable PNG of the given size: an RGB gradient, so a resize changes its bytes.</summary>
    /// <param name="width">The width in pixels, at least 1.</param>
    /// <param name="height">The height in pixels, at least 1.</param>
    /// <returns>The PNG's bytes.</returns>
    public static byte[] PngImage(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);

        var header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
        header[8] = 8; // bit depth
        header[9] = 2; // colour type: RGB

        using var pixels = new MemoryStream();
        using (var deflate = new ZLibStream(pixels, CompressionLevel.Fastest, leaveOpen: true))
        {
            var row = new byte[1 + (width * 3)];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    row[1 + (x * 3)] = (byte)(x * 255 / Math.Max(1, width - 1));
                    row[2 + (x * 3)] = (byte)(y * 255 / Math.Max(1, height - 1));
                    row[3 + (x * 3)] = (byte)((x + y) % 256);
                }

                deflate.Write(row);
            }
        }

        using var png = new MemoryStream();
        png.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        WriteChunk(png, "IHDR", header);
        WriteChunk(png, "IDAT", pixels.ToArray());
        WriteChunk(png, "IEND", []);
        return png.ToArray();
    }

    /// <summary>Tells whether bytes are a WebP image (<c>RIFF</c> … <c>WEBP</c>).</summary>
    /// <param name="bytes">The bytes.</param>
    public static bool IsWebp(byte[] bytes) =>
        bytes.Length > 12 && bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8) && bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8);

    /// <summary>Returns the SHA-256 of bytes, lowercase hex, as Files reports it.</summary>
    /// <param name="bytes">The bytes.</param>
    /// <returns>The digest.</returns>
    public static string Sha256(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    private static void WriteChunk(Stream png, string type, byte[] data)
    {
        Span<byte> number = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(number, data.Length);
        png.Write(number);

        var typed = new byte[4 + data.Length];
        Encoding.ASCII.GetBytes(type, typed);
        data.CopyTo(typed, 4);
        png.Write(typed);

        var crc = 0xFFFFFFFFu;
        foreach (var value in typed)
            crc = CrcTable[(crc ^ value) & 0xFF] ^ (crc >> 8);
        BinaryPrimitives.WriteUInt32BigEndian(number, crc ^ 0xFFFFFFFFu);
        png.Write(number);
    }
}
