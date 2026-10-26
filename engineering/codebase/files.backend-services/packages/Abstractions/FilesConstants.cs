namespace WoW.Two.Sdk.Beta.Be.Files;

/// <summary>Holds the limits every form of the Files contract applies.</summary>
public static class FilesConstants
{
    /// <summary>Holds the longest file name the contract accepts, in characters.</summary>
    public const int MaxNameLength = 255;

    /// <summary>Holds the length of a SHA-256 digest in lowercase hex.</summary>
    public const int Sha256HexLength = 64;

    /// <summary>Holds the most files one list call returns.</summary>
    public const int MaxPageSize = 200;
}
