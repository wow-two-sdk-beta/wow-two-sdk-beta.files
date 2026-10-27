using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using WoW.Two.Sdk.Beta.Be.Files.Module;
using WoW.Two.Sdk.Beta.Be.Files.Module.Callers;
using WoW.Two.Sdk.Beta.Be.Files.References;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Files.Testing.Files;
using WoW.Two.Sdk.Beta.Be.Files.Uploads;
using WoW.Two.Sdk.Beta.Be.Files.Urls;
using WoW.Two.Sdk.Beta.Be.Storage.Core;
using WoW.Two.Sdk.Beta.Be.Storage.FileSystem;
using WoW.Two.Sdk.Beta.Be.Testing.Data.Migrations;

namespace Files.Tests.Conformance.Files;

/// <summary>Form 1: the module wired as a consumer wires it, over a throwaway SQLite file and blob folder, with the
/// caller's tenant set per scope.</summary>
public sealed class ModuleFilesFixture : IAsyncLifetime
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"files-conformance-{Guid.NewGuid():N}");
    private readonly List<AsyncServiceScope> scopes = [];
    private ServiceProvider services = null!;

    private string ConnectionString => $"Data Source={Path.Combine(root, "files.db")}";

    /// <summary>Opens the brokers as one tenant reaches them; the scope lives until the fixture ends.</summary>
    public FilesBrokerSet For(string tenantId)
    {
        var scope = services.CreateAsyncScope();
        lock (scopes)
            scopes.Add(scope);
        scope.ServiceProvider.GetRequiredService<TenantCallerService>().TenantId = tenantId;
        return new FilesBrokerSet(
            scope.ServiceProvider.GetRequiredService<IFileUploadsBroker>(),
            scope.ServiceProvider.GetRequiredService<IFileStoreBroker>(),
            scope.ServiceProvider.GetRequiredService<IFileReferencesBroker>(),
            scope.ServiceProvider.GetRequiredService<IFileUrlsBroker>());
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        Directory.CreateDirectory(root);
        await using (var migrator = MigratorHarness.CreateSqlite(ConnectionString, typeof(FilesModuleAssembly).Assembly))
            await migrator.Runner.ApplyPendingAsync("test", CancellationToken.None);

        var collection = new ServiceCollection().AddLogging();
        collection.AddSingleton<IBlobRepository>(new LocalFileBlobRepository(Path.Combine(root, "blobs")));
        collection.AddScoped<TenantCallerService>();
        collection.AddScoped<IFilesCallerService>(provider => provider.GetRequiredService<TenantCallerService>());
        collection.AddFiles().Configure(options =>
        {
            options.ConnectionString = ConnectionString;
            options.TempPath = Path.Combine(root, "tmp");
            options.SweepInterval = TimeSpan.Zero;
            options.Delivery.BaseUrl = new Uri("https://files.example.test");
            options.Delivery.SigningKey = "conformance-signing-key-0123456789abcdef";
        });
        services = collection.BuildServiceProvider(validateScopes: true);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        foreach (var scope in scopes)
            await scope.DisposeAsync();
        await services.DisposeAsync();
        SqliteConnection.ClearAllPools();
        Directory.Delete(root, recursive: true);
    }

    private sealed class TenantCallerService : IFilesCallerService
    {
        public string TenantId { get; set; } = "default";

        public FilesCallerModel Current => new() { TenantId = TenantId, SubjectId = "conformance" };
    }
}
