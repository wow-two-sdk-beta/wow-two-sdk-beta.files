using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Npgsql;
using WoW.Two.Sdk.Beta.Be.Data.Abstractions;
using WoW.Two.Sdk.Beta.Be.Data.Dapper;
using WoW.Two.Sdk.Beta.Be.Data.EntityFrameworkCore;
using WoW.Two.Sdk.Beta.Be.Data.Migrations.Bespoke;
using WoW.Two.Sdk.Beta.Be.Observability.HealthChecks;
using WoW.Two.Sdk.Beta.Be.Files.Audit;
using WoW.Two.Sdk.Beta.Be.Files.Module.Audit;
using WoW.Two.Sdk.Beta.Be.Files.Module.Blobs;
using WoW.Two.Sdk.Beta.Be.Files.Module.Callers;
using WoW.Two.Sdk.Beta.Be.Files.Module.Delivery;
using WoW.Two.Sdk.Beta.Be.Files.Module.Imports;
using WoW.Two.Sdk.Beta.Be.Files.Module.Persistence;
using WoW.Two.Sdk.Beta.Be.Files.Module.References;
using WoW.Two.Sdk.Beta.Be.Files.Module.Store;
using WoW.Two.Sdk.Beta.Be.Files.Module.Sweeps;
using WoW.Two.Sdk.Beta.Be.Files.Module.Uploads;
using WoW.Two.Sdk.Beta.Be.Files.Module.Urls;
using WoW.Two.Sdk.Beta.Be.Files.Module.Variants;
using WoW.Two.Sdk.Beta.Be.Files.References;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Files.Uploads;
using WoW.Two.Sdk.Beta.Be.Files.Urls;
using WoW.Two.Sdk.Beta.Be.Media.Images;

namespace WoW.Two.Sdk.Beta.Be.Files.Module;

/// <summary>Registers the in-process Files module (Form 1).</summary>
public static class FilesModuleServiceCollectionExtensions
{
    /// <summary>Registers the Files brokers over the module's store, the delivery seam the content host serves through,
    /// image variants through core's image service, imports with their tokens, the hourly sweep and the store's
    /// readiness check.</summary>
    /// <remarks>The host registers the byte store, core <c>IBlobRepository</c> (<c>AddLocalBlobStorage</c>,
    /// <c>AddS3BlobStorage</c>, …); replace <see cref="IFilesCallerService"/> to resolve callers another way, and bind
    /// <see cref="FileImportOptions"/> to change the import token's lifetime. Variants decode within core's image limits
    /// (<c>Media:Images</c>, <see cref="ImageOptions"/>).</remarks>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The options builder, through which the host sets the engine and the connection string; they are read on
    /// first use.</returns>
    public static OptionsBuilder<FilesModuleOptions> AddFiles(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        services.AddHttpContextAccessor();
        services.AddDbContext<FilesDbContext>((provider, options) => options.UseFilesStore(Store(provider)));
        services.TryAddScoped<FilesCallerOverride>();
        services.TryAddScoped<IFilesCallerService, PrincipalFilesCallerService>();
        services.TryAddSingleton<FileBlobLocks>();
        services.TryAddSingleton<SignedFileUrlIssuer>();
        services.AddImageProcessing();
        services.AddScoped<FileContentReader>();
        services.AddScoped<FileBlobWriter>();
        services.AddScoped<FileVariantReader>();
        services.AddScoped<FileVariantService>();
        services.AddScoped<FileAuditWriter>();
        services.AddScoped<IFileUploadsBroker, ModuleFileUploadsBroker>();
        services.AddScoped<IFileStoreBroker, ModuleFileStoreBroker>();
        services.AddScoped<IFileReferencesBroker, ModuleFileReferencesBroker>();
        services.AddScoped<IFileUrlsBroker, ModuleFileUrlsBroker>();
        services.AddScoped<IFilesDeliveryService, FilesDeliveryService>();
        services.AddScoped<IFileAuditBroker, ModuleFileAuditBroker>();
        services.AddScoped<IFilesSweepService, FilesSweepService>();
        services.AddOptions<FileImportOptions>();
        services.TryAddSingleton<IFileImportTokenService, FileImportTokenService>();
        services.AddScoped<IFilesImportsService, FilesImportsService>();
        services.AddHostedService<FilesSweepBackgroundService>();
        services.AddHealthChecks().AddDatabaseReadinessCheck<FilesDbContext>();

        return services.AddOptions<FilesModuleOptions>()
            .Validate(options => options.Provider is DatabaseProvider.Postgres or DatabaseProvider.Sqlite, "FilesModuleOptions.Provider must be Postgres or Sqlite.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ConnectionString), "FilesModuleOptions.ConnectionString must be set.")
            .Validate(options => FileBlobKeyMapper.IsValidTenant(options.DefaultTenantId), $"FilesModuleOptions.DefaultTenantId must match {FilesConstants.TenantIdPattern}.")
            .Validate(
                options => FileVariantWidthMapper.IsValid(options.DefaultPolicy) && options.Policies.Values.All(FileVariantWidthMapper.IsValid),
                $"Every Files policy's Variants must be widths of 1 to {FileVariantWidthMapper.MaxWidth} pixels, and its VariantQuality 1 to 100.");
    }

    /// <summary>Registers the module's schema as the be sdk migration set <see cref="FilesMigrations.SetName"/>: the
    /// embedded SQL of <see cref="FilesModuleOptions.Provider"/>, applied to the store at
    /// <see cref="FilesModuleOptions.ConnectionString"/>.</summary>
    /// <remarks>The set keeps its own history (<c>files_migration_history</c>) and lock beside the host's other sets; the be
    /// sdk's boot and deploy helpers (<c>MigrateBespokeOnStartupAsync</c>, <c>MigrateBespokeForDeployAsync</c>) create the
    /// store's database when it is missing and apply the set with the others. Call it beside <see cref="AddFiles"/>.</remarks>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">An optional hook over the set's <see cref="MigrationOptions"/>, such as its version.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddFilesMigrations(this IServiceCollection services, Action<MigrationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // The set's own database and connections: the module's store, on the module's engine.
        services.AddKeyedSingleton(FilesMigrations.SetName, (provider, _) => new DatabaseSettings { ConnectionString = Store(provider).ConnectionString });
        services.AddKeyedSingleton(FilesMigrations.SetName, (provider, _) => NpgsqlDataSource.Create(Store(provider).ConnectionString));
        services.AddKeyedSingleton<IDbConnectionFactory>(FilesMigrations.SetName, (provider, _) => Store(provider) is { Provider: DatabaseProvider.Sqlite } sqlite
            ? new SqliteConnectionFactory(sqlite.ConnectionString)
            : new DataSourceConnectionFactory(provider.GetRequiredKeyedService<NpgsqlDataSource>(FilesMigrations.SetName)));

        // The dialect follows the store; the caller's hook runs after it.
        services.AddOptions<MigrationOptions>(FilesMigrations.SetName)
            .Configure<IOptions<FilesModuleOptions>>((migrations, store) => migrations.Provider = store.Value.Provider);
        return services.AddDatabaseBespokeMigrationSet(FilesMigrations.SetName, provider => FilesMigrations.CreateBroker(Store(provider).Provider), configure);
    }

    /// <summary>Reads the module's settings.</summary>
    private static FilesModuleOptions Store(IServiceProvider provider) => provider.GetRequiredService<IOptions<FilesModuleOptions>>().Value;
}
