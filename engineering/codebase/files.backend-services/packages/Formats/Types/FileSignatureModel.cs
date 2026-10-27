namespace WoW.Two.Sdk.Beta.Be.Files.Formats.Types;

/// <summary>Represents one magic-byte signature of a file type: parts at fixed offsets that must all match.</summary>
public sealed record FileSignatureModel
{
    /// <summary>Gets the parts, each at its own offset.</summary>
    public required IReadOnlyList<FileSignaturePart> Parts { get; init; }

    /// <summary>Gets whether the signature names its type from bytes alone; when false it only confirms a type a name
    /// claims, because a more common type shares it (a DOCX is a ZIP).</summary>
    public bool Detects { get; init; } = true;

    /// <summary>Gets how many bytes the parts compare; a longer signature is the more specific one.</summary>
    public int Length => Parts.Sum(part => part.Bytes.Length);

    /// <summary>Gets whether a match is strong enough to contradict a type with no signature: four bytes or more, or
    /// a byte no text holds.</summary>
    public bool IsDistinctive => Length >= 4 || Parts.Any(part => part.Bytes.Span.ContainsAnyExceptInRange((byte)0x20, (byte)0x7E));

    /// <summary>Compares the signature with a file's leading bytes.</summary>
    /// <param name="head">The file's leading bytes.</param>
    /// <returns>Whether every part matches, a part differs, or the bytes end too early to tell.</returns>
    public FileSignatureMatch Match(ReadOnlySpan<byte> head)
    {
        var truncated = false;
        foreach (var part in Parts)
        {
            var expected = part.Bytes.Span;
            if (part.Offset >= head.Length)
            {
                truncated = true;
                continue;
            }

            var available = head[part.Offset..];
            if (available.Length < expected.Length)
            {
                if (!available.SequenceEqual(expected[..available.Length]))
                    return FileSignatureMatch.NoMatch;
                truncated = true;
                continue;
            }

            if (!available[..expected.Length].SequenceEqual(expected))
                return FileSignatureMatch.NoMatch;
        }

        return truncated ? FileSignatureMatch.Unknown : FileSignatureMatch.Match;
    }

    /// <inheritdoc />
    public override string ToString() => (Detects ? string.Empty : "~") + string.Join(' ', Parts);
}
