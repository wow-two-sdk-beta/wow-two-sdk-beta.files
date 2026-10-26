namespace WoW.Two.Sdk.Beta.Be.Files;

/// <summary>Holds the HTTP routes of the Files contract, shared by the service, the server package and the client.</summary>
public static class FilesRouteConstants
{
    /// <summary>Holds the API root, relative to the service root.</summary>
    public const string Api = "api/files/v1";

    /// <summary>Holds the files resource.</summary>
    public const string Files = Api + "/files";

    /// <summary>Holds the hosting route prefix of a private file served through a signed URL.</summary>
    public const string SignedFile = "f";

    /// <summary>Holds the hosting route prefix of a public, immutable file.</summary>
    public const string PublicFile = "m";

    /// <summary>Holds the hosting route prefix of a share page.</summary>
    public const string Share = "s";

    /// <summary>Holds the hosting route prefix of a file request page.</summary>
    public const string FileRequest = "r";

    /// <summary>Holds the header that makes a creating call safe to retry.</summary>
    public const string IdempotencyKeyHeader = "Idempotency-Key";
}
