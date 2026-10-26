namespace WoW.Two.Sdk.Beta.Be.Files;

/// <summary>Holds the OAuth scopes a Files instance accepts; Identity issues them (contract C3).</summary>
public static class FilesScopeConstants
{
    /// <summary>Holds the scope that reads files, references and signed URLs.</summary>
    public const string Read = "files.read";

    /// <summary>Holds the scope that uploads, changes and deletes files.</summary>
    public const string Write = "files.write";

    /// <summary>Holds the scope that creates and revokes shares.</summary>
    public const string Share = "files.share";

    /// <summary>Holds the scope that reads usage and audit and sets policies.</summary>
    public const string Admin = "files.admin";

    /// <summary>Holds the scope that acts for a user or organization through the delegation headers.</summary>
    public const string Delegate = "files.delegate";

    /// <summary>Holds the scope that runs tenants on a shared files host.</summary>
    public const string Tenants = "files.tenants";
}
