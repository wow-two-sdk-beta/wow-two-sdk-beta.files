namespace WoW.Two.Sdk.Beta.Be.Files.Module.Sweeps;

/// <summary>Defines the sweep that purges what the store no longer keeps: deleted files past retention, expired
/// files, and bytes no file uses.</summary>
public interface IFilesSweepService
{
    /// <summary>Runs one sweep across every tenant.</summary>
    /// <param name="cancellationToken">The token that stops the sweep between items.</param>
    /// <returns>What was purged.</returns>
    Task<FileSweepResultModel> SweepAsync(CancellationToken cancellationToken = default);
}
