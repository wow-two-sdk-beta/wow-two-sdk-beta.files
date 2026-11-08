namespace WoW.Two.Sdk.Beta.Be.Files.Module.Imports;

/// <summary>Holds the settings of imports; the service binds them from <c>Files:Imports</c>.</summary>
public sealed class FileImportOptions
{
    /// <summary>Holds the section the service binds the settings from.</summary>
    public const string SectionName = "Files:Imports";

    /// <summary>Gets or sets how long an import token lives. Default 6 hours.</summary>
    public TimeSpan TokenLifetime { get; set; } = TimeSpan.FromHours(6);
}
