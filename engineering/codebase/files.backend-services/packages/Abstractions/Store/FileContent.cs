namespace WoW.Two.Sdk.Beta.Be.Files.Store;

/// <summary>Represents an open read of a file's bytes; the caller disposes <see cref="Content"/>.</summary>
/// <param name="Content">The bytes, positioned at the start of the requested range.</param>
/// <param name="ContentType">The file's content type.</param>
/// <param name="Length">The length of the returned bytes.</param>
/// <param name="TotalLength">The length of the whole file.</param>
/// <param name="Range">The range served; none for the whole file.</param>
public sealed record FileContent(Stream Content, string ContentType, long Length, long TotalLength, ByteRange? Range);
