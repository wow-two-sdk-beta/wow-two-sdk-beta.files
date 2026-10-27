namespace WoW.Two.Sdk.Beta.Be.Files.Formats.Types;

/// <summary>Refers to how a file's leading bytes compare with a signature.</summary>
public enum FileSignatureMatch
{
    /// <summary>A part differs.</summary>
    NoMatch = 0,

    /// <summary>Every part matches.</summary>
    Match,

    /// <summary>The bytes end before the signature does, and every part they reach matches.</summary>
    Unknown,
}
