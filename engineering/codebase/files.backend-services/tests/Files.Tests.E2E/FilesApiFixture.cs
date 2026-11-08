using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using WoW.Two.Sdk.Beta.Be.Identity.Testing;
using WoW.Two.Sdk.Beta.Be.Testing;

namespace Files.Tests.E2E;

/// <summary>Boots the real API and its SQL migrations over a throwaway SQLite file, trusting two fake issuers as two
/// tenants, each with its own audience.</summary>
public sealed class FilesApiFixture : IAsyncLifetime
{
    /// <summary>Holds the public host the test target allows, beside <c>localhost</c>, as a deployed target's <c>AllowedHosts</c> does.</summary>
    public const string PublicHost = "files.example.test";

    /// <summary>Holds the key the test host signs file URLs with.</summary>
    public const string SigningKey = "e2e-signing-key-0123456789abcdef0123456789";

    /// <summary>Holds a test API key with every Files scope, in the <c>e2e</c> tenant.</summary>
    public const string AdminKey = "files_E2eAdminKey0123456789abcdefghijk";

    /// <summary>Holds a test API key with <c>files.read</c> only, in the <c>e2e</c> tenant.</summary>
    public const string ReadKey = "files_E2eReadKey0123456789abcdefghijkl";

    /// <summary>Holds a test API key of the <c>tiny</c> app, whose policy accepts 8 bytes.</summary>
    public const string TinyKey = "files_E2eTinyKey0123456789abcdefghijkl";

    /// <summary>Holds the audience the <c>tokens</c> tenant's tokens name: its own host, with the slash.</summary>
    public const string Audience = "https://files.tokens.test/";

    /// <summary>Holds the audience the <c>other</c> tenant's tokens name.</summary>
    public const string OtherAudience = "https://files.other.test/";

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
                ["Files:Policies:tiny:MaxBytes"] = "8",
                ["Files:Auth:Issuers:0:Issuer"] = Issuer.Issuer,
                ["Files:Auth:Issuers:0:TenantId"] = "tokens",
                ["Files:Auth:Issuers:0:Audiences:0"] = Audience,
                ["Files:Auth:Issuers:0:Jwks"] = JwksOf(Issuer),
                ["Files:Auth:Issuers:1:Issuer"] = OtherIssuer.Issuer,
                ["Files:Auth:Issuers:1:TenantId"] = "other",
                ["Files:Auth:Issuers:1:Audiences:0"] = OtherAudience,
                ["Files:Auth:Issuers:1:Jwks"] = JwksOf(OtherIssuer),
                ["Files:Auth:ApiKeys:0:Id"] = "e2e-admin",
                ["Files:Auth:ApiKeys:0:Sha256"] = Sha256(AdminKey),
                ["Files:Auth:ApiKeys:0:TenantId"] = "e2e",
                ["Files:Auth:ApiKeys:0:Scopes:0"] = "files.read",
                ["Files:Auth:ApiKeys:0:Scopes:1"] = "files.write",
                ["Files:Auth:ApiKeys:0:Scopes:2"] = "files.admin",
                ["Files:Auth:ApiKeys:1:Id"] = "e2e-read",
                ["Files:Auth:ApiKeys:1:Sha256"] = Sha256(ReadKey),
                ["Files:Auth:ApiKeys:1:TenantId"] = "e2e",
                ["Files:Auth:ApiKeys:1:Scopes:0"] = "files.read",
                ["Files:Auth:ApiKeys:2:Id"] = "e2e-tiny",
                ["Files:Auth:ApiKeys:2:Sha256"] = Sha256(TinyKey),
                ["Files:Auth:ApiKeys:2:TenantId"] = "e2e",
                ["Files:Auth:ApiKeys:2:AppId"] = "tiny",
                ["Files:Auth:ApiKeys:2:Scopes:0"] = "files.write",
                ["Logging:LogLevel:Default"] = "Warning",
            }),
        };
    }

    /// <summary>Gets the issuer the <c>tokens</c> tenant trusts.</summary>
    public FakeTokenIssuer Issuer { get; } = new("https://identity.tokens.test/");

    /// <summary>Gets the issuer the <c>other</c> tenant trusts.</summary>
    public FakeTokenIssuer OtherIssuer { get; } = new("https://identity.other.test/");

    /// <summary>Writes an issuer's public key as the JSON Web Key Set a trusted issuer's settings take.</summary>
    public static string JwksOf(FakeTokenIssuer issuer)
    {
        var key = JsonWebKeyConverter.ConvertFromRSASecurityKey((RsaSecurityKey)issuer.AsTrustedIssuer().SigningKeys![0]);
        return $$"""{"keys":[{"kty":"RSA","use":"sig","alg":"RS256","kid":"{{key.Kid}}","n":"{{key.N}}","e":"{{key.E}}"}]}""";
    }

    /// <summary>Returns the SHA-256 the service stores for a key.</summary>
    public static string Sha256(string key) =>
        Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(key)));

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
        Issuer.Dispose();
        OtherIssuer.Dispose();

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
