namespace WoW.Two.Sdk.Beta.Be.Files.Formats;

/// <summary>Holds the limits the file-type checks work within.</summary>
public static class FileTypeConstants
{
    /// <summary>Holds how many leading bytes a caller reads for detection: enough for an ISO image's signature at 32769.</summary>
    public const int HeadLength = 64 * 1024;

    /// <summary>Holds the content type of bytes no registry row describes.</summary>
    public const string OctetStream = "application/octet-stream";
}
