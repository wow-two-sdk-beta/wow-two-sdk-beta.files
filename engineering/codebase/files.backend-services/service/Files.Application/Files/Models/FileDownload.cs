using WoW.Two.Sdk.Beta.Be.Files.Store;

namespace Files.Application.Files.Models;

/// <summary>Contains authorized file bytes and the original file's download metadata.</summary>
public sealed record FileDownload(FileModel File, FileContent Content)
{
    /// <summary>Identifies the size required to answer a failed range request.</summary>
    public const string UnsatisfiedRangeLength = "unsatisfiedRangeLength";
}
