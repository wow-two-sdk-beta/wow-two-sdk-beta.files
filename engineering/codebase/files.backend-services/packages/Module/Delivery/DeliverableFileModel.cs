using WoW.Two.Sdk.Beta.Be.Files.Formats.Types;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Files.Urls;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Delivery;

/// <summary>Represents a file a content-host request may receive: what to serve, and how.</summary>
public sealed record DeliverableFileModel
{
    /// <summary>Gets the owning tenant.</summary>
    public required string TenantId { get; init; }

    /// <summary>Gets the file's id.</summary>
    public required FileId Id { get; init; }

    /// <summary>Gets the safe name, for <c>Content-Disposition</c>.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the stored content type.</summary>
    public required string ContentType { get; init; }

    /// <summary>Gets the size in bytes.</summary>
    public required long Size { get; init; }

    /// <summary>Gets the SHA-256 of the bytes; it is the entity tag.</summary>
    public required string Sha256 { get; init; }

    /// <summary>Gets what the type can do when opened; active and executable types are always downloads.</summary>
    public required FileRisk Risk { get; init; }

    /// <summary>Gets the disposition the URL asked for.</summary>
    public required FileDisposition Disposition { get; init; }

    /// <summary>Gets when a signed URL stops working; none for a public URL, which never does.</summary>
    public DateTimeOffset? ExpiresAtUtc { get; init; }
}
