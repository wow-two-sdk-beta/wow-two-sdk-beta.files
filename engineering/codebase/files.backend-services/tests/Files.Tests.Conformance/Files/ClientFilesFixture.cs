using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WoW.Two.Sdk.Beta.Be.Files.Audit;
using WoW.Two.Sdk.Beta.Be.Files.Client;
using WoW.Two.Sdk.Beta.Be.Files.References;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Files.Testing.Files;
using WoW.Two.Sdk.Beta.Be.Files.Uploads;
using WoW.Two.Sdk.Beta.Be.Files.Urls;
using WoW.Two.Sdk.Beta.Be.Testing;

namespace Files.Tests.Conformance.Files;

/// <summary>Forms 2 and 3: the Client wired as a consumer wires it, by API key, against the service host — two keys,
/// two tenants.</summary>
/// <remarks>By default the host runs in process over a throwaway SQLite file and blob folder. With
/// <c>CONFORMANCE_BASE_URL</c> set, the Client calls that running host or image, which must accept
/// <see cref="TenantKey"/> and <see cref="OtherTenantKey"/> (the conformance workflow configures them).</remarks>
public sealed class ClientFilesFixture : IAsyncLifetime
{
    /// <summary>Holds the test-only key of the tenant under test.</summary>
    public const string TenantKey = "files-conformance-key-tenant-a-0123456789";

    /// <summary>Holds the test-only key of the other tenant.</summary>
    public const string OtherTenantKey = "files-conformance-key-tenant-b-0123456789";

    private readonly string root = Path.Combine(Path.GetTempPath(), $"files-conformance-client-{Guid.NewGuid():N}");
    private readonly Uri? runningHost = Environment.GetEnvironmentVariable(Greetings.ServiceFixture.BaseUrlVariable) is { Length: > 0 } url ? new Uri(url) : null;
    private readonly List<ServiceProvider> providers = [];
    private WebApiTestHost<Program>? host;

    /// <summary>Gets the brokers as the tenant under test reaches them.</summary>
    public FilesBrokerSet Tenant { get; private set; } = null!;

    /// <summary>Gets the brokers as the other tenant reaches them.</summary>
    public FilesBrokerSet OtherTenant { get; private set; } = null!;

    /// <summary>Gets the clock the service stamps with: the test host's, or the real one for a running image.</summary>
    public TimeProvider Clock => host?.Clock ?? TimeProvider.System;

    Task IAsyncLifetime.InitializeAsync()
    {
        if (runningHost is null)
        {
            Directory.CreateDirectory(root);
            host = new WebApiTestHost<Program>
            {
                ConfigureConfigurationHook = configuration => configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["DatabaseSettings:ConnectionString"] = $"Data Source={Path.Combine(root, "files.db")}",
                    ["Storage:Root"] = Path.Combine(root, "blobs"),
                    ["Files:Delivery:BaseUrl"] = "http://localhost",
                    ["Files:Delivery:SigningKey"] = "conformance-signing-key-0123456789abcdef",
                    ["Files:Auth:ApiKeys:0:Id"] = "conformance-a",
                    ["Files:Auth:ApiKeys:0:Sha256"] = Sha256(TenantKey),
                    ["Files:Auth:ApiKeys:0:TenantId"] = "conformance-a",
                    ["Files:Auth:ApiKeys:0:Scopes:0"] = "files.read",
                    ["Files:Auth:ApiKeys:0:Scopes:1"] = "files.write",
                    ["Files:Auth:ApiKeys:0:Scopes:2"] = "files.admin",
                    ["Files:Auth:ApiKeys:1:Id"] = "conformance-b",
                    ["Files:Auth:ApiKeys:1:Sha256"] = Sha256(OtherTenantKey),
                    ["Files:Auth:ApiKeys:1:TenantId"] = "conformance-b",
                    ["Files:Auth:ApiKeys:1:Scopes:0"] = "files.read",
                    ["Files:Auth:ApiKeys:1:Scopes:1"] = "files.write",
                    ["Files:Auth:ApiKeys:1:Scopes:2"] = "files.admin",
                    ["Logging:LogLevel:Default"] = "Warning",
                }),
            };
        }

        Tenant = Brokers(TenantKey);
        OtherTenant = Brokers(OtherTenantKey);
        return Task.CompletedTask;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        foreach (var provider in providers)
            await provider.DisposeAsync();
        if (host is null)
            return;

        await host.DisposeAsync();
        SqliteConnection.ClearAllPools();
        Directory.Delete(root, recursive: true);
    }

    /// <summary>Returns the SHA-256 the service stores for a key.</summary>
    public static string Sha256(string key) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(key)));

    private FilesBrokerSet Brokers(string apiKey)
    {
        var services = new ServiceCollection().AddLogging();
        var client = services.AddFilesClient(options =>
        {
            options.BaseAddress = runningHost ?? host!.Server.BaseAddress;
            options.ApiKey = apiKey;
        });
        if (host is not null)
            client.ConfigurePrimaryHttpMessageHandler(host.Server.CreateHandler);

        var provider = services.BuildServiceProvider(validateScopes: true);
        providers.Add(provider);
        return new FilesBrokerSet(
            provider.GetRequiredService<IFileUploadsBroker>(),
            provider.GetRequiredService<IFileStoreBroker>(),
            provider.GetRequiredService<IFileReferencesBroker>(),
            provider.GetRequiredService<IFileUrlsBroker>(),
            provider.GetRequiredService<IFileAuditBroker>());
    }
}
