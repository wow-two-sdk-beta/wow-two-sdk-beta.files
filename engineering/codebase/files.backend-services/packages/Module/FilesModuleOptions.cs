namespace WoW.Two.Sdk.Beta.Be.Files.Module;

/// <summary>Holds the settings of the in-process Files module.</summary>
public sealed class FilesModuleOptions
{
    /// <summary>Gets or sets the SQLite connection string of the store that holds the Files tables.</summary>
    public string ConnectionString { get; set; } = string.Empty;
}
