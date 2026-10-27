using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using WoW.Two.Sdk.Beta.Be.Files.Module;
using WoW.Two.Sdk.Beta.Be.Files.Module.Callers;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Files.Uploads;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;
using WoW.Two.Sdk.Beta.Be.Storage.Core;
using WoW.Two.Sdk.Beta.Be.Storage.FileSystem;
using WoW.Two.Sdk.Beta.Be.Testing.Data.Migrations;

namespace Files.Tests.Integration.Files;

/// <summary>The module wired as a consumer wires it — a throwaway SQLite file its migrations build, a temp blob
/// folder, a fake clock — with the caller set per scope.</summary>
public sealed class ModuleHarness : IAsyncDisposable
{
    public static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x0D, 0x49, 0x48, 0x44, 0x52, 1, 2, 3, 4];
    public static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10, 0x4A, 0x46, 0x49, 0x46, 0, 1, 1, 0, 0, 1, 0, 1, 0, 0];
    public static readonly byte[] Pdf = "%PDF-1.7\n%âã\n1 0 obj\n<<>>\nendobj\n"u8.ToArray();
    public static readonly byte[] Exe = [0x4D, 0x5A, 0x90, 0, 3, 0, 0, 0, 4, 0, 0, 0, 0xFF, 0xFF, 0, 0];

    private readonly string root = Path.Combine(Path.GetTempPath(), $"files-integration-{Guid.NewGuid():N}");
    private readonly ServiceProvider services;

    private ModuleHarness(Action<FilesModuleOptions>? configure)
    {
        Directory.CreateDirectory(root);
        var collection = new ServiceCollection().AddLogging().AddSingleton<TimeProvider>(Clock);
        collection.AddSingleton<IBlobRepository>(new LocalFileBlobRepository(BlobRoot));
        collection.AddScoped<TestFilesCallerService>();
        collection.AddScoped<IFilesCallerService>(provider => provider.GetRequiredService<TestFilesCallerService>());
        collection.AddFiles().Configure(options =>
        {
            options.ConnectionString = ConnectionString;
            options.TempPath = TempRoot;
            options.SweepInterval = TimeSpan.Zero;
            configure?.Invoke(options);
        });
        services = collection.BuildServiceProvider(validateScopes: true);
    }

    /// <summary>Gets the clock the module stamps files with.</summary>
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 10, 27, 9, 0, 0, TimeSpan.Zero));

    /// <summary>Gets the blob folder.</summary>
    public string BlobRoot => Path.Combine(root, "blobs");

    /// <summary>Gets the folder uploads spool through.</summary>
    public string TempRoot => Path.Combine(root, "tmp");

    private string ConnectionString => $"Data Source={Path.Combine(root, "files.db")}";

    /// <summary>Builds the harness over a migrated store.</summary>
    public static async Task<ModuleHarness> CreateAsync(Action<FilesModuleOptions>? configure = null)
    {
        var harness = new ModuleHarness(configure);
        await using (var migrator = MigratorHarness.CreateSqlite(harness.ConnectionString, typeof(FilesModuleAssembly).Assembly))
            await migrator.Runner.ApplyPendingAsync("test", CancellationToken.None);
        return harness;
    }

    /// <summary>Opens a scope acting for a caller.</summary>
    public AsyncServiceScope As(string tenantId = "t1", string? appId = null, string? subjectId = "user-1", string? organizationId = null)
    {
        var scope = services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<TestFilesCallerService>().Current =
            new FilesCallerModel { TenantId = tenantId, AppId = appId, SubjectId = subjectId, OrganizationId = organizationId };
        return scope;
    }

    /// <summary>Uploads bytes as a caller.</summary>
    public async Task<Result<FileModel>> UploadAsync(byte[] bytes, string name, string tenantId = "t1", string? appId = null, string? idempotencyKey = null, DateTimeOffset? expiresAtUtc = null)
    {
        await using var scope = As(tenantId, appId);
        using var content = new MemoryStream(bytes);
        return await scope.ServiceProvider.GetRequiredService<IFileUploadsBroker>()
            .UploadAsync(content, new UploadSpec { Name = name, IdempotencyKey = idempotencyKey, ExpiresAtUtc = expiresAtUtc });
    }

    /// <summary>Runs a store call as a caller.</summary>
    public async Task<T> StoreAsync<T>(Func<IFileStoreBroker, Task<T>> call, string tenantId = "t1")
    {
        await using var scope = As(tenantId);
        return await call(scope.ServiceProvider.GetRequiredService<IFileStoreBroker>());
    }

    /// <summary>Runs a call against a service of the module, as a caller.</summary>
    public async Task<T> WithAsync<TService, T>(Func<TService, Task<T>> call, string tenantId = "t1") where TService : notnull
    {
        await using var scope = As(tenantId);
        return await call(scope.ServiceProvider.GetRequiredService<TService>());
    }

    /// <summary>Lists every stored blob file.</summary>
    public IReadOnlyList<string> BlobFiles() =>
        Directory.Exists(BlobRoot) ? Directory.GetFiles(BlobRoot, "*", SearchOption.AllDirectories) : [];

    /// <summary>Returns the SHA-256 of bytes, lowercase hex.</summary>
    public static string Sha256(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    public async ValueTask DisposeAsync()
    {
        await services.DisposeAsync();
        SqliteConnection.ClearAllPools();
        Directory.Delete(root, recursive: true);
    }

    /// <summary>Returns a success value, failing the test with the error otherwise.</summary>
    public static T Value<T>(Result<T> result) where T : notnull =>
        result is Result<T>.Success success ? success.Value : throw new Xunit.Sdk.XunitException(((Result<T>.Failure)result).Error.Message);

    /// <summary>Returns a failure's error, failing the test on success.</summary>
    public static AppError Error<T>(Result<T> result) where T : notnull =>
        result is Result<T>.Failure failure ? failure.Error : throw new Xunit.Sdk.XunitException("The call succeeded.");

    private sealed class TestFilesCallerService : IFilesCallerService
    {
        public FilesCallerModel Current { get; set; } = new() { TenantId = "t1" };
    }
}
