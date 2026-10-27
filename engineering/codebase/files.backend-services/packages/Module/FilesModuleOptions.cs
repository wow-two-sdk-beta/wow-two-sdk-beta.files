using WoW.Two.Sdk.Beta.Be.Files.Module.Policies;

namespace WoW.Two.Sdk.Beta.Be.Files.Module;

/// <summary>Holds the settings of the in-process Files module.</summary>
public sealed class FilesModuleOptions
{
    /// <summary>Gets or sets the SQLite connection string of the store that holds the Files tables.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Gets or sets the tenant a call acts for when neither the request nor the ambient tenant names one;
    /// an owner instance (<c>Single</c> mode) keeps every file under it.</summary>
    public string DefaultTenantId { get; set; } = "default";

    /// <summary>Gets or sets the policy of an app with no entry in <see cref="Policies"/>.</summary>
    public FilePolicyOptions DefaultPolicy { get; set; } = new();

    /// <summary>Gets the policies by app id.</summary>
    public Dictionary<string, FilePolicyOptions> Policies { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets or sets the folder uploads spool through while they are hashed and checked; the system temp
    /// folder when empty.</summary>
    public string? TempPath { get; set; }

    /// <summary>Gets or sets how often the sweep purges deleted, expired and unused files; zero turns the background
    /// sweep off.</summary>
    public TimeSpan SweepInterval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>Returns the policy of an app.</summary>
    /// <param name="appId">The app; none takes the default policy.</param>
    /// <returns>The app's policy, or the default one.</returns>
    public FilePolicyOptions PolicyFor(string? appId) =>
        appId is not null && Policies.TryGetValue(appId, out var policy) ? policy : DefaultPolicy;
}
