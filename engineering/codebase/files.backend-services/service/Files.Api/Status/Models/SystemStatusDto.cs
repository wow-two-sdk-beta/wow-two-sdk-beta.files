namespace Files.Api.Status.Models;

/// <summary>Represents the liveness and identity of the service.</summary>
public sealed record SystemStatusDto
{
    /// <summary>Gets the service name.</summary>
    public required string Service { get; init; }

    /// <summary>Gets the liveness state.</summary>
    public required string Status { get; init; }

    /// <summary>Gets the informational version of the running build, with its commit.</summary>
    public required string Version { get; init; }

    /// <summary>Gets whether this process makes WebP variants: <c>ok</c> or <c>unavailable</c>.</summary>
    public required string Imaging { get; init; }
}
