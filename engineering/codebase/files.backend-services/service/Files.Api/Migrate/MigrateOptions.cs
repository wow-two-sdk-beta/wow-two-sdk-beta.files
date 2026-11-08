namespace Files.Api.Migrate;

/// <summary>Holds the mover's fixed limits; the defaults are the command's, tests narrow them.</summary>
public sealed class MigrateOptions
{
    /// <summary>Gets the handler the Files client sends through; <see langword="null"/> uses a socket handler.</summary>
    public HttpMessageHandler? Handler { get; init; }

    /// <summary>Gets the waits before each retry of a 5xx or network error; three retries by default.</summary>
    public IReadOnlyList<TimeSpan> RetryDelays { get; init; } = [TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(8)];

    /// <summary>Gets the most bytes of one row held in memory before it spills to a temporary file: 64 MiB.</summary>
    public long MemoryLimit { get; init; } = 64L * 1024 * 1024;

    /// <summary>Gets the folder spilled rows are written to.</summary>
    public string TempDirectory { get; init; } = Path.GetTempPath();

    /// <summary>Gets the clock progress and retries run on.</summary>
    public TimeProvider Time { get; init; } = TimeProvider.System;

    /// <summary>Gets the shortest time between two progress lines: 2 s.</summary>
    public TimeSpan ProgressInterval { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>Gets the longest one request may take, an upload of a large file included.</summary>
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromMinutes(30);

    /// <summary>Gets the <c>limit</c> each map page asks for.</summary>
    public int MapPageSize { get; init; } = 500;
}
