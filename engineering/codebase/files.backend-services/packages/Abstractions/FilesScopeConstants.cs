namespace WoW.Two.Sdk.Beta.Be.Files;

/// <summary>Holds the OAuth scopes a Files instance accepts: what the calling app may ask of it. Identity puts every scope
/// a client may use on Files into each of its tokens (t8 C3).</summary>
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

    /// <summary>Holds the scope that runs tenants on a shared files host; only the operator's issuer grants it.</summary>
    public const string Tenants = "files.tenants";
}
