namespace WoW.Two.Sdk.Beta.Be.Files.Client;

/// <summary>Sets whom a Files call acts as, for work a person started that later runs as the service.</summary>
/// <remarks>With Identity set, a call acts for the person when one is on it and as the service otherwise; work accepted
/// for a person and run later, such as a queued job, runs as the service, with the person recorded, never with their
/// token.</remarks>
public static class FilesClientCalls
{
    private static readonly AsyncLocal<bool> ServiceFlag = new();

    /// <summary>Gets whether the current call runs under <see cref="AsService"/>.</summary>
    internal static bool IsService => ServiceFlag.Value;

    /// <summary>Makes every Files call in the scope send the service's own client-credentials token, even when a person is
    /// on the current call; the scope flows into awaited calls, and disposing it restores what was set before.</summary>
    /// <example><code>using (FilesClientCalls.AsService())
    ///     await store.DeleteAsync(id, cancellationToken);</code></example>
    /// <returns>The scope; dispose it to end the override.</returns>
    public static IDisposable AsService()
    {
        var previous = ServiceFlag.Value;
        ServiceFlag.Value = true;
        return new ServiceScope(previous);
    }

    /// <summary>Represents one <see cref="AsService"/> scope, which puts back the value it replaced.</summary>
    private sealed class ServiceScope(bool previous) : IDisposable
    {
        private bool disposed;

        /// <inheritdoc />
        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            ServiceFlag.Value = previous;
        }
    }
}
