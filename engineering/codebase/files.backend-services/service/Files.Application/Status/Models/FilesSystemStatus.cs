namespace Files.Application.Status.Models;

/// <summary>Describes the file server's build and preview generation availability.</summary>
public sealed record FilesSystemStatus(string Version, string Imaging);
