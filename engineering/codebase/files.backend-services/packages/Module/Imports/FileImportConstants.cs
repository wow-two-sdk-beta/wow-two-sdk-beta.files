namespace WoW.Two.Sdk.Beta.Be.Files.Module.Imports;

/// <summary>Holds the names and limits of imports: a product's legacy files moving into a Files tenant
/// (<c>product/features/imports.md</c>).</summary>
public static class FileImportConstants
{
    /// <summary>Holds the imports resource, relative to the service root.</summary>
    public const string Route = FilesRouteConstants.Api + "/imports";

    /// <summary>Holds the header an import token travels in; it never travels as <c>Authorization: Bearer</c>.</summary>
    public const string TokenHeader = "X-Import-Token";

    /// <summary>Holds the marker every import token starts with.</summary>
    public const string TokenMarker = "fimp_";

    /// <summary>Holds the HKDF info the token key is derived from <c>Files:Delivery:SigningKey</c> under.</summary>
    public const string TokenKeyInfo = "files-import-token";

    /// <summary>Holds the state of an import that takes pushes; the only state in P1.</summary>
    public const string OpenState = "open";

    /// <summary>Holds the most sources one import lists.</summary>
    public const int MaxSources = 50;

    /// <summary>Holds the shape of a source id.</summary>
    public const string SourceIdPattern = "^[a-z0-9][a-z0-9._-]{0,99}$";

    /// <summary>Holds the longest source key accepted, in characters.</summary>
    public const int MaxKeyLength = 1024;

    /// <summary>Holds the most items one map page returns.</summary>
    public const int MaxMapPageSize = 1000;

    /// <summary>Holds the prefix of the subject an import's pushes are recorded under: <c>import:{id}</c>.</summary>
    public const string SubjectPrefix = "import:";
}
