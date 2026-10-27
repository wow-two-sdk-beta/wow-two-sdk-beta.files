using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using WoW.Two.Sdk.Beta.Be.Data.EntityFrameworkCore.Sqlite;
using WoW.Two.Sdk.Beta.Be.Observability.HealthChecks;
using WoW.Two.Sdk.Beta.Be.Files.Greetings;
using WoW.Two.Sdk.Beta.Be.Files.Module.Blobs;
using WoW.Two.Sdk.Beta.Be.Files.Module.Callers;
using WoW.Two.Sdk.Beta.Be.Files.Module.Delivery;
using WoW.Two.Sdk.Beta.Be.Files.Module.Greetings;
using WoW.Two.Sdk.Beta.Be.Files.Module.Persistence;
using WoW.Two.Sdk.Beta.Be.Files.Module.References;
using WoW.Two.Sdk.Beta.Be.Files.Module.Store;
using WoW.Two.Sdk.Beta.Be.Files.Module.Sweeps;
using WoW.Two.Sdk.Beta.Be.Files.Module.Uploads;
using WoW.Two.Sdk.Beta.Be.Files.Module.Urls;
using WoW.Two.Sdk.Beta.Be.Files.References;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Files.Uploads;
using WoW.Two.Sdk.Beta.Be.Files.Urls;

namespace WoW.Two.Sdk.Beta.Be.Files.Module;

/// <summary>Registers the in-process Files module (Form 1).</summary>
public static class FilesModuleServiceCollectionExtensions
{
    /// <summary>Registers the Files brokers over the module's store, the delivery seam the content host serves through, the
    /// hourly sweep and the store's readiness check.</summary>
    /// <remarks>The host registers the byte store, core <c>IBlobRepository</c> (<c>AddLocalBlobStorage</c>,
    /// <c>AddS3BlobStorage</c>, …); replace <see cref="IFilesCallerService"/> to resolve callers another way.</remarks>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The options builder, through which the host sets the connection string; it is read on first use.</returns>
    public static OptionsBuilder<FilesModuleOptions> AddFiles(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        services.AddHttpContextAccessor();
        services.AddDbContext<FilesDbContext>((provider, options) =>
            options.UseSqliteConventional(provider.GetRequiredService<IOptions<FilesModuleOptions>>().Value.ConnectionString));
        services.TryAddScoped<IFilesCallerService, HttpFilesCallerService>();
        services.TryAddSingleton<FileBlobLocks>();
        services.TryAddSingleton<SignedFileUrlIssuer>();
        services.AddScoped<FileContentReader>();
        services.AddScoped<IGreetingsBroker, ModuleGreetingsBroker>();
        services.AddScoped<IFileUploadsBroker, ModuleFileUploadsBroker>();
        services.AddScoped<IFileStoreBroker, ModuleFileStoreBroker>();
        services.AddScoped<IFileReferencesBroker, ModuleFileReferencesBroker>();
        services.AddScoped<IFileUrlsBroker, ModuleFileUrlsBroker>();
        services.AddScoped<IFilesDeliveryService, FilesDeliveryService>();
        services.AddScoped<IFilesSweepService, FilesSweepService>();
        services.AddHostedService<FilesSweepBackgroundService>();
        services.AddHealthChecks().AddDatabaseReadinessCheck<FilesDbContext>();

        return services.AddOptions<FilesModuleOptions>()
            .Validate(options => !string.IsNullOrWhiteSpace(options.ConnectionString), "FilesModuleOptions.ConnectionString must be set.")
            .Validate(options => FileBlobKeyMapper.IsValidTenant(options.DefaultTenantId), $"FilesModuleOptions.DefaultTenantId must match {FilesConstants.TenantIdPattern}.");
    }
}
