namespace WoW.Two.Sdk.Beta.Be.Files.Store;

/// <summary>Decides who may read a file.</summary>
public enum FileVisibility
{
    /// <summary>Only callers the tenant authorizes, through signed URLs.</summary>
    Private = 0,

    /// <summary>Members of the file's organization.</summary>
    Organization,

    /// <summary>Anyone, at an immutable public URL on the tenant's host.</summary>
    Public,
}
