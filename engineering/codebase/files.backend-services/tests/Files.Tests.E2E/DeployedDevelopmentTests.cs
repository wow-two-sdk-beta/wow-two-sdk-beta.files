using System.Net;
using AwesomeAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WoW.Two.Sdk.Beta.Be.Files.Module;
using WoW.Two.Sdk.Beta.Be.Testing;

namespace Files.Tests.E2E;

/// <summary>Boots the API as deployed dev does: .NET <c>Development</c>, with only what Wheelhouse supplies. The repo's
/// <c>appsettings.Development.json</c> must add no local value: file links point at the app site, and no API key from
/// the repo opens the API (conventions: environment-files.md § Settings).</summary>
public sealed class DeployedDevelopmentTests : IAsyncLifetime
{
    private const string AppSite = "https://files-dev.example.test/";
    private const string LocalKey = "files_LocalDevKey0123456789abcdefghijk";

    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"files-e2e-dev-{Guid.NewGuid():N}.db");
    private readonly string storageRoot = Path.Combine(Path.GetTempPath(), $"files-e2e-dev-blobs-{Guid.NewGuid():N}");
    private readonly WebApiTestHost<Program> host;

    public DeployedDevelopmentTests()
    {
        host = new WebApiTestHost<Program>
        {
            ConfigureConfigurationHook = configuration => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabaseSettings:Provider"] = "Sqlite",
                ["DatabaseSettings:ConnectionString"] = $"Data Source={databasePath}",
                ["Storage:Root"] = storageRoot,
                ["Files:Delivery:SigningKey"] = "vault-signing-key-0123456789abcdef0123456789",
                ["Sites:app:Url"] = AppSite,
                ["Logging:LogLevel:Default"] = "Warning",
            }),
        };
    }

    [Fact]
    public void Delivery_ShouldLinkFilesOnTheAppSite_WhenNoSettingNamesAContentHost()
    {
        var options = host.Services.GetRequiredService<IOptions<FilesModuleOptions>>().Value;

        options.Delivery.BaseUrl.Should().Be(new Uri(AppSite));
    }

    [Fact]
    public void Configuration_ShouldHoldNoLoopbackAddressOrApiKey_UnderFiles()
    {
        var files = host.Services.GetRequiredService<IConfiguration>().GetSection("Files").AsEnumerable().ToList();

        files.Select(setting => setting.Value).Should().NotContain(value => value != null && (value.Contains("localhost", StringComparison.OrdinalIgnoreCase) || value.Contains("127.0.0.1", StringComparison.Ordinal) || value.Contains("[::1]", StringComparison.Ordinal)));
        files.Should().NotContain(setting => setting.Key.StartsWith("Files:Auth:ApiKeys", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Api_ShouldRefuseTheLocalRunKey()
    {
        using var client = host.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", LocalKey);

        using var response = await client.GetAsync("/api/files/v1/files");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    Task IAsyncLifetime.InitializeAsync()
    {
        using var client = host.CreateClient();
        return Task.CompletedTask;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await host.DisposeAsync();
        SqliteConnection.ClearAllPools();
        foreach (var path in new[] { databasePath, databasePath + "-shm", databasePath + "-wal" })
            File.Delete(path);
        if (Directory.Exists(storageRoot))
            Directory.Delete(storageRoot, recursive: true);
    }
}
