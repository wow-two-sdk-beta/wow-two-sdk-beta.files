namespace WoW.Two.Sdk.Beta.Be.Files.Module.Blobs;

/// <summary>Serializes the work on one blob — an upload reusing or storing it, the sweep deleting it — through 64
/// striped locks keyed by SHA-256, so the sweep never deletes bytes an upload is reusing.</summary>
/// <remarks>Holds within one process; P1 runs every role in one process per store.</remarks>
internal sealed class FileBlobLocks
{
    private readonly SemaphoreSlim[] stripes = Enumerable.Range(0, 64).Select(_ => new SemaphoreSlim(1, 1)).ToArray();

    /// <summary>Waits for the lock of a digest; dispose the result to release it.</summary>
    public async Task<IDisposable> AcquireAsync(string sha256, CancellationToken cancellationToken)
    {
        var stripe = stripes[Convert.ToInt32(sha256[..2], 16) % stripes.Length];
        await stripe.WaitAsync(cancellationToken);
        return new Release(stripe);
    }

    private sealed class Release(SemaphoreSlim stripe) : IDisposable
    {
        private int released;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref released, 1) == 0)
                stripe.Release();
        }
    }
}
