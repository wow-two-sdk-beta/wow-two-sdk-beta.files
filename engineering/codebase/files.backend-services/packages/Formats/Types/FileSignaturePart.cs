namespace WoW.Two.Sdk.Beta.Be.Files.Formats.Types;

/// <summary>Represents bytes a signature expects at one offset.</summary>
/// <param name="Offset">Where the bytes start, from the start of the file.</param>
/// <param name="Bytes">The expected bytes.</param>
public readonly record struct FileSignaturePart(int Offset, ReadOnlyMemory<byte> Bytes)
{
    /// <inheritdoc />
    public override string ToString() => $"{Offset}:{Convert.ToHexString(Bytes.Span)}";
}
