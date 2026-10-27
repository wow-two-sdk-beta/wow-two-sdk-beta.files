namespace WoW.Two.Sdk.Beta.Be.Files.Module.Sweeps;

/// <summary>Represents what one sweep purged.</summary>
/// <param name="Files">The file rows purged: deleted past retention, or expired.</param>
/// <param name="Blobs">The stored bytes purged, no longer used by any file.</param>
public sealed record FileSweepResultModel(int Files, int Blobs);
