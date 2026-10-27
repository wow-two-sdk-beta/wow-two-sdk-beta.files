namespace WoW.Two.Sdk.Beta.Be.Files.Formats.Types;

/// <summary>Refers to what a file of a type can do when a browser or an operating system opens it.</summary>
public enum FileRisk
{
    /// <summary>Opens as data.</summary>
    None = 0,

    /// <summary>Runs script when a browser renders it inline, as HTML, SVG and XML do; it is only ever served as a download.</summary>
    Active,

    /// <summary>Runs as a program, script or macro when opened.</summary>
    Executable,
}
