namespace WoW.Two.Sdk.Beta.Be.Files.Store;

/// <summary>Selects bytes of a file, as an HTTP <c>Range</c> header does.</summary>
/// <param name="From">The first byte, zero-based.</param>
/// <param name="To">The last byte, inclusive; the end of the file when none.</param>
public readonly record struct ByteRange(long From, long? To);
