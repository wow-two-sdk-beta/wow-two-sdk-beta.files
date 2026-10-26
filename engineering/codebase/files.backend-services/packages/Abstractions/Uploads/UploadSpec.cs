using WoW.Two.Sdk.Beta.Be.Files.Store;

namespace WoW.Two.Sdk.Beta.Be.Files.Uploads;

/// <summary>Describes a file to upload; the app policy decides what is accepted.</summary>
public sealed record UploadSpec
{
    /// <summary>Gets the file name the uploader gave.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the content type the uploader claimed; the bytes decide in the end.</summary>
    public string? ContentType { get; init; }

    /// <summary>Gets the app whose policy applies.</summary>
    public string? AppId { get; init; }

    /// <summary>Gets who may read the file.</summary>
    public FileVisibility Visibility { get; init; } = FileVisibility.Private;

    /// <summary>Gets the key that makes a retried upload return the first one's file.</summary>
    public string? IdempotencyKey { get; init; }

    /// <summary>Gets when the file expires (UTC); none keeps it while referenced.</summary>
    public DateTimeOffset? ExpiresAtUtc { get; init; }
}
