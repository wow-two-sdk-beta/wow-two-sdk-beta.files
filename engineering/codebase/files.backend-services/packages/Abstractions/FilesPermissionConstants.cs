namespace WoW.Two.Sdk.Beta.Be.Files;

/// <summary>Holds the permissions a person needs beside the app's scope (<see cref="FilesScopeConstants"/>). Each
/// tenant's Identity grants them to its roles, and keeps only these in a token for Files (its <c>files</c> permission
/// namespace); Files names no role.</summary>
public static class FilesPermissionConstants
{
    /// <summary>Holds the permission that reads files, references and signed URLs.</summary>
    public const string Read = "files:read";

    /// <summary>Holds the permission that uploads, changes and deletes files.</summary>
    public const string Write = "files:write";

    /// <summary>Holds the permission that creates and revokes shares.</summary>
    public const string Share = "files:share";

    /// <summary>Holds the permission that reads usage and audit and sets policies.</summary>
    public const string Admin = "files:admin";
}
