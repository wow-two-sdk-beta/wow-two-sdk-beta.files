using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using WoW.Two.Sdk.Beta.Be.Testing;

namespace Files.Tests.E2E;

/// <summary>Boots the real API and its SQL migrations over a throwaway SQLite file.</summary>
public sealed class FilesApiFixture : IAsyncLifetime
{
    /// <summary>Holds the public host the test target allows, beside <c>localhost</c>, as a deployed target's <c>AllowedHosts</c> does.</summary>
    public const string PublicHost = "files.example.test";

    /// <summary>Holds the key the test host signs file URLs with.</summary>
    public const string SigningKey = "e2e-signing-key-0123456789abcdef0123456789";

    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"files-e2e-{Guid.NewGuid():N}.db");
    private readonly string storageRoot = Path.Combine(Path.GetTempPath(), $"files-e2e-blobs-{Guid.NewGuid():N}");

    public FilesApiFixture()
    {
        Host = new WebApiTestHost<Program>
        {
            ConfigureConfigurationHook = configuration => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabaseSettings:ConnectionString"] = $"Data Source={databasePath}",
                ["AllowedHosts"] = PublicHost + ";localhost",
                ["Storage:Root"] = storageRoot,
                ["Files:Delivery:BaseUrl"] = "http://localhost",
                ["Files:Delivery:SigningKey"] = SigningKey,
                ["Logging:LogLevel:Default"] = "Warning",
            }),
        };
    }

    /// <summary>Gets the booted API host.</summary>
    public WebApiTestHost<Program> Host { get; }

    Task IAsyncLifetime.InitializeAsync()
    {
        // Building a client starts the host, which creates the database and applies the migrations.
        using var client = Host.CreateClient();
        return Task.CompletedTask;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await Host.DisposeAsync();

        // Pooled connections hold the file open, so release them before the files go.
        SqliteConnection.ClearAllPools();
        foreach (var path in new[] { databasePath, databasePath + "-shm", databasePath + "-wal" })
            File.Delete(path);
        if (Directory.Exists(storageRoot))
            Directory.Delete(storageRoot, recursive: true);
    }
}

/// <summary>Shares one <see cref="FilesApiFixture"/> across the whole E2E run.</summary>
[CollectionDefinition(Name)]
public sealed class FilesApiCollection : ICollectionFixture<FilesApiFixture>
{
    /// <summary>The collection name every E2E class joins.</summary>
    public const string Name = "files-api";
}
