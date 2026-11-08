using WoW.Two.Sdk.Beta.Be.Data.Migrations.Bespoke;
using WoW.Two.Sdk.Beta.Be.Files.Module;
using WoW.Two.Sdk.Beta.Be.Storage.Core;
using WoW.Two.Sdk.Beta.Be.Storage.FileSystem;
using WoW.Two.Sdk.Beta.Be.Web.Json;

namespace Files.Api.Configurations;

public static partial class HostConfiguration
{
    /// <summary>Hosts the Form 1 module over the service's PostgreSQL store (SQLite when the settings choose it) and local
    /// blob folder, with the module's migration set on the same engine.</summary>
    private static WebApplicationBuilder AddFilesModule(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IBlobRepository>(provider => new LocalFileBlobRepository(
            ResolveStorageRoot(provider.GetRequiredService<IConfiguration>(), provider.GetRequiredService<IHostEnvironment>())));
        builder.Services.AddFiles()
            .BindConfiguration(FilesSection)
            // Resolved lazily, so a test host's configuration overrides still reach the store.
            .Configure<IConfiguration>((options, configuration) =>
            {
                options.Provider = ResolveProvider(configuration);
                options.ConnectionString = ResolveConnectionString(configuration);
            })
            .PostConfigure<IConfiguration>((options, configuration) =>
            {
                // Without its own content host, the service links files on the app site Wheelhouse derives.
                if (options.Delivery.BaseUrl is null && configuration["Sites:app:Url"] is { Length: > 0 } site)
                    options.Delivery.BaseUrl = new Uri(site);
            });
        builder.Services.AddFilesMigrations(migrations =>
            migrations.Version = MigrationVersionMapper.ToVersion(typeof(HostConfiguration).Assembly));
        return builder;
    }

    /// <summary>Registers controllers on the SDK wire contract.</summary>
    private static WebApplicationBuilder AddControllers(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllersWithSdkJson();
        return builder;
    }

    /// <summary>Holds the section the module's settings bind from: default tenant, policies by app, sweep interval,
    /// delivery.</summary>
    private const string FilesSection = "Files";

    /// <summary>Resolves the folder the bytes live in: <c>Storage:Root</c>, relative to the content root; <c>data/blobs</c> when unset.</summary>
    private static string ResolveStorageRoot(IConfiguration configuration, IHostEnvironment environment) =>
        Path.GetFullPath(configuration["Storage:Root"] is { Length: > 0 } root ? root : Path.Combine("data", "blobs"), environment.ContentRootPath);

    /// <summary>Resolves the store's engine from <c>DatabaseSettings:Provider</c>: PostgreSQL unless it names <c>Sqlite</c>.</summary>
    private static DatabaseProvider ResolveProvider(IConfiguration configuration) =>
        configuration["DatabaseSettings:Provider"] switch
        {
            null or "" => DatabaseProvider.Postgres,
            var name when Enum.TryParse<DatabaseProvider>(name, ignoreCase: true, out var provider) && Enum.IsDefined(provider) => provider,
            var name => throw new InvalidOperationException($"DatabaseSettings:Provider '{name}' is unknown. Set Postgres (the default) or Sqlite."),
        };

    /// <summary>Resolves the connection string: the <c>DB_CONNECTION</c> alias wins over <c>DatabaseSettings:ConnectionString</c>.</summary>
    private static string ResolveConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration["DB_CONNECTION"];
        if (string.IsNullOrWhiteSpace(connectionString))
            connectionString = configuration["DatabaseSettings:ConnectionString"];

        return string.IsNullOrWhiteSpace(connectionString)
            ? throw new InvalidOperationException("Database connection string not found. Set DB_CONNECTION or DatabaseSettings:ConnectionString.")
            : connectionString;
    }
}
