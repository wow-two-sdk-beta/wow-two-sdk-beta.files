using WoW.Two.Sdk.Beta.Be.Data.Abstractions;
using WoW.Two.Sdk.Beta.Be.Data.Dapper;
using WoW.Two.Sdk.Beta.Be.Data.EntityFrameworkCore;
using WoW.Two.Sdk.Beta.Be.Data.Migrations.Bespoke;
using WoW.Two.Sdk.Beta.Be.Files.Module;
using WoW.Two.Sdk.Beta.Be.Storage.Core;
using WoW.Two.Sdk.Beta.Be.Storage.FileSystem;
using WoW.Two.Sdk.Beta.Be.Web.Json;

namespace Files.Api.Configurations;

public static partial class HostConfiguration
{
    /// <summary>Hosts the Form 1 module over the service's SQLite store and local blob folder, with the migrator over the module's SQL.</summary>
    private static WebApplicationBuilder AddFilesModule(this WebApplicationBuilder builder)
    {
        // Resolved lazily, so a test host's configuration overrides still reach the connection string.
        builder.Services.AddSingleton(provider => new DatabaseSettings
        {
            ConnectionString = ResolveConnectionString(provider.GetRequiredService<IConfiguration>()),
        });
        builder.Services.AddSingleton<IDbConnectionFactory>(provider =>
            new SqliteConnectionFactory(provider.GetRequiredService<DatabaseSettings>().ConnectionString));
        builder.Services.AddSingleton<IBlobRepository>(provider => new LocalFileBlobRepository(
            ResolveStorageRoot(provider.GetRequiredService<IConfiguration>(), provider.GetRequiredService<IHostEnvironment>())));
        builder.Services.AddFiles()
            .BindConfiguration(FilesSection)
            .Configure<DatabaseSettings>((options, database) => options.ConnectionString = database.ConnectionString);
        builder.Services.AddDatabaseBespokeMigrations(typeof(FilesModuleAssembly).Assembly, migrations =>
        {
            migrations.Provider = DatabaseProvider.Sqlite;
            migrations.Version = MigrationVersionMapper.ToVersion(typeof(HostConfiguration).Assembly);
        });
        return builder;
    }

    /// <summary>Registers controllers on the SDK wire contract.</summary>
    private static WebApplicationBuilder AddControllers(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllersWithSdkJson();
        return builder;
    }

    /// <summary>Holds the section the module's settings bind from: default tenant, policies by app, sweep interval.</summary>
    private const string FilesSection = "Files";

    /// <summary>Resolves the folder the bytes live in: <c>Storage:Root</c>, relative to the content root; <c>data/blobs</c> when unset.</summary>
    private static string ResolveStorageRoot(IConfiguration configuration, IHostEnvironment environment) =>
        Path.GetFullPath(configuration["Storage:Root"] is { Length: > 0 } root ? root : Path.Combine("data", "blobs"), environment.ContentRootPath);

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
