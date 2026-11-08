using Files.Tests.Lanes;
using Microsoft.Extensions.DependencyInjection;
using WoW.Two.Sdk.Beta.Be.Files.Audit;
using WoW.Two.Sdk.Beta.Be.Files.Module;
using WoW.Two.Sdk.Beta.Be.Files.Module.Callers;
using WoW.Two.Sdk.Beta.Be.Files.Module.Policies;
using WoW.Two.Sdk.Beta.Be.Files.References;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Files.Testing.Files;
using WoW.Two.Sdk.Beta.Be.Files.Uploads;
using WoW.Two.Sdk.Beta.Be.Files.Urls;
using WoW.Two.Sdk.Beta.Be.Storage.Core;
using WoW.Two.Sdk.Beta.Be.Storage.FileSystem;

namespace Files.Tests.Conformance.Files;

/// <summary>Form 1: the module wired as a consumer wires it, over a throwaway store of one lane and a blob folder, with
/// the caller's tenant set per scope.</summary>
/// <param name="lane">The lane whose store the fixture's tests share.</param>
public abstract class ModuleFilesFixture(IFilesLane lane) : IAsyncLifetime
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"files-conformance-{Guid.NewGuid():N}");
    private readonly List<AsyncServiceScope> scopes = [];
    private FilesTestStore? store;
    private ServiceProvider? services;

    /// <summary>Opens the brokers as one tenant reaches them; the scope lives until the fixture ends.</summary>
    public FilesBrokerSet For(string tenantId)
    {
        var scope = (services ?? throw new InvalidOperationException("The fixture's store did not start.")).CreateAsyncScope();
        lock (scopes)
            scopes.Add(scope);
        scope.ServiceProvider.GetRequiredService<TenantCallerService>().TenantId = tenantId;
        return new FilesBrokerSet(
            scope.ServiceProvider.GetRequiredService<IFileUploadsBroker>(),
            scope.ServiceProvider.GetRequiredService<IFileStoreBroker>(),
            scope.ServiceProvider.GetRequiredService<IFileReferencesBroker>(),
            scope.ServiceProvider.GetRequiredService<IFileUrlsBroker>(),
            scope.ServiceProvider.GetRequiredService<IFileAuditBroker>());
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        // A PostgreSQL lane without Docker starts nothing: its tests are reported skipped.
        if (lane is PostgresLane && PostgresLane.SkipReason is not null)
            return;

        Directory.CreateDirectory(root);
        var created = store = await lane.CreateAsync(root);
        await created.MigrateAsync();

        var collection = new ServiceCollection().AddLogging();
        collection.AddSingleton<IBlobRepository>(new LocalFileBlobRepository(Path.Combine(root, "blobs")));
        collection.AddScoped<TenantCallerService>();
        collection.AddScoped<IFilesCallerService>(provider => provider.GetRequiredService<TenantCallerService>());
        collection.AddFiles().Configure(options =>
        {
            created.ApplyTo(options);
            options.TempPath = Path.Combine(root, "tmp");
            options.SweepInterval = TimeSpan.Zero;
            options.Delivery.BaseUrl = new Uri("https://files.example.test");
            options.Delivery.SigningKey = "conformance-signing-key-0123456789abcdef";
            var variants = new FilePolicyOptions();
            variants.Variants.AddRange(FileVariantsConformance.Widths);
            options.Policies[FileVariantsConformance.App] = variants;
        });
        services = collection.BuildServiceProvider(validateScopes: true);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        foreach (var scope in scopes)
            await scope.DisposeAsync();
        if (services is not null)
            await services.DisposeAsync();
        if (store is not null)
            await store.DisposeAsync();
        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);
    }

    private sealed class TenantCallerService : IFilesCallerService
    {
        public string TenantId { get; set; } = "default";

        public FilesCallerModel Current => new() { TenantId = TenantId, SubjectId = "conformance" };
    }
}

/// <summary>The module over a SQLite file.</summary>
public sealed class ModuleFilesSqliteFixture() : ModuleFilesFixture(SqliteLane.Instance);

/// <summary>The module over a PostgreSQL database in the assembly's container.</summary>
/// <param name="lane">The assembly's container.</param>
public sealed class ModuleFilesPostgresFixture(PostgresLane lane) : ModuleFilesFixture(lane);
