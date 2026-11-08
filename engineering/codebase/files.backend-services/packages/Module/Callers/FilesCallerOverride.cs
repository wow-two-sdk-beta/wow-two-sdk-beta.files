namespace WoW.Two.Sdk.Beta.Be.Files.Module.Callers;

/// <summary>Holds a caller that wins over the request's principal for the rest of a scope's call, such as an import
/// pushing into its own tenant as <c>import:{id}</c>; <see cref="PrincipalFilesCallerService"/> reads it first.</summary>
/// <remarks>Scoped: one per request, so an override never leaks into another call.</remarks>
public sealed class FilesCallerOverride
{
    /// <summary>Gets the caller set by <see cref="Use"/>; none while no override is active.</summary>
    public FilesCallerModel? Current { get; private set; }

    /// <summary>Acts as <paramref name="caller"/> until the returned handle is disposed, which restores the caller before it.</summary>
    /// <param name="caller">The caller the scope's Files calls act for.</param>
    /// <returns>The handle that ends the override.</returns>
    public IDisposable Use(FilesCallerModel caller)
    {
        ArgumentNullException.ThrowIfNull(caller);
        var previous = Current;
        Current = caller;
        return new Restore(this, previous);
    }

    /// <summary>Restores the caller an override replaced.</summary>
    private sealed class Restore(FilesCallerOverride owner, FilesCallerModel? previous) : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            owner.Current = previous;
        }
    }
}
