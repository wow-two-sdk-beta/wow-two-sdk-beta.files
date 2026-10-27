namespace WoW.Two.Sdk.Beta.Be.Files.Module.Callers;

/// <summary>Defines who the current Files call acts for; a host replaces it to resolve callers its own way.</summary>
public interface IFilesCallerService
{
    /// <summary>Gets the current caller.</summary>
    FilesCallerModel Current { get; }
}
