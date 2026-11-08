using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using WoW.Two.Sdk.Beta.Be.Files.Module;
using WoW.Two.Sdk.Beta.Be.Files.Module.Imports;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;
using WoW.Two.Sdk.Beta.Be.Storage.Core;
using WoW.Two.Sdk.Beta.Be.Storage.FileSystem;
using WoW.Two.Sdk.Beta.Be.Testing.Data.Migrations;

namespace Files.Tests.Integration.Imports;

/// <summary>The module wired as the service wires it — the default caller service, no principal — so an import's push
/// acts only through its caller override; a throwaway SQLite file its migrations build, a temp blob folder, a fake clock.</summary>
public sealed class ImportsHarness : IAsyncDisposable
{
    /// <summary>Holds the signing key import tokens derive their key from.</summary>
    public const string SigningKey = "imports-signing-key-0123456789abcdef0123";

    public static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x0D, 0x49, 0x48, 0x44, 0x52, 1, 2, 3, 4];
    public static readonly byte[] Pdf = "%PDF-1.7\n%âã\n1 0 obj\n<<>>\nendobj\n"u8.ToArray();

    private readonly string root = Path.Combine(Path.GetTempPath(), $"files-imports-{Guid.NewGuid():N}");
    private readonly ServiceProvider services;

    private ImportsHarness(string signingKey)
    {
        Directory.CreateDirectory(root);
        var collection = new ServiceCollection().AddLogging().AddSingleton<TimeProvider>(Clock);
        collection.AddSingleton<IBlobRepository>(new LocalFileBlobRepository(Path.Combine(root, "blobs")));
        collection.AddFiles().Configure(options =>
        {
            options.ConnectionString = ConnectionString;
            options.TempPath = Path.Combine(root, "tmp");
            options.SweepInterval = TimeSpan.Zero;
            options.Delivery.SigningKey = signingKey;
        });
        services = collection.BuildServiceProvider(validateScopes: true);
    }

    /// <summary>Gets the clock tokens and rows are stamped with.</summary>
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 11, 8, 9, 0, 0, TimeSpan.Zero));

    private string ConnectionString => $"Data Source={Path.Combine(root, "files.db")}";

    /// <summary>Builds the harness over a migrated store.</summary>
    public static async Task<ImportsHarness> CreateAsync(string signingKey = SigningKey)
    {
        var harness = new ImportsHarness(signingKey);
        await using (var migrator = MigratorHarness.CreateSqlite(harness.ConnectionString, typeof(FilesModuleAssembly).Assembly))
            await migrator.Runner.ApplyPendingAsync("test", CancellationToken.None);
        return harness;
    }

    /// <summary>Opens a scope, as one request does.</summary>
    public AsyncServiceScope Scope() => services.CreateAsyncScope();

    /// <summary>Gets the token service.</summary>
    public IFileImportTokenService Tokens => services.GetRequiredService<IFileImportTokenService>();

    /// <summary>Runs a call against the imports service in its own scope.</summary>
    public async Task<T> ImportsAsync<T>(Func<IFilesImportsService, Task<T>> call)
    {
        await using var scope = Scope();
        return await call(scope.ServiceProvider.GetRequiredService<IFilesImportsService>());
    }

    /// <summary>Opens an import of the <c>ocharo</c> tenant.</summary>
    public async Task<FileImportTicketModel> OpenAsync(params string[] sources) =>
        Value(await ImportsAsync(imports => imports.OpenAsync(new FileImportSpec { Tenant = "ocharo", Sources = sources.Length > 0 ? sources : ["brand-media"] })));

    /// <summary>Pushes bytes into an import, claiming their own SHA-256 unless told otherwise.</summary>
    public Task<Result<FileImportPushResultModel>> PushAsync(string importId, string key, byte[] bytes, string? sha256 = null, string? reference = null, string source = "brand-media") =>
        ImportsAsync(imports =>
        {
            var content = new MemoryStream(bytes);
            return imports.PushAsync(importId, content, new FileImportPushSpec { Source = source, Key = key, Sha256 = sha256 ?? Sha256(bytes), Reference = reference });
        });

    /// <summary>Reads rows straight from the store; each row maps column names to values.</summary>
    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> RowsAsync(string sql)
    {
        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<IReadOnlyDictionary<string, object?>>();
        while (await reader.ReadAsync())
            rows.Add(Enumerable.Range(0, reader.FieldCount).ToDictionary(i => reader.GetName(i), i => reader.IsDBNull(i) ? null : reader.GetValue(i)));
        return rows;
    }

    /// <summary>Returns the SHA-256 of bytes, lowercase hex.</summary>
    public static string Sha256(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    /// <summary>Returns a success value, failing the test with the error otherwise.</summary>
    public static T Value<T>(Result<T> result) where T : notnull =>
        result is Result<T>.Success success ? success.Value : throw new Xunit.Sdk.XunitException(((Result<T>.Failure)result).Error.Message);

    public async ValueTask DisposeAsync()
    {
        await services.DisposeAsync();
        SqliteConnection.ClearAllPools();
        Directory.Delete(root, recursive: true);
    }
}
