using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using WoW.Two.Sdk.Beta.Be.Data.EntityFrameworkCore.Sqlite;
using WoW.Two.Sdk.Beta.Be.Observability.HealthChecks;
using WoW.Two.Sdk.Beta.Be.Files.Greetings;
using WoW.Two.Sdk.Beta.Be.Files.Module.Greetings;
using WoW.Two.Sdk.Beta.Be.Files.Module.Persistence;

namespace WoW.Two.Sdk.Beta.Be.Files.Module;

/// <summary>Registers the in-process Files module (Form 1).</summary>
public static class FilesModuleServiceCollectionExtensions
{
    /// <summary>Registers <see cref="IGreetingsBroker"/> over the module's store, and the store's readiness check.</summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The options builder, through which the host sets the connection string; it is read on first use.</returns>
    public static OptionsBuilder<FilesModuleOptions> AddFiles(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        services.AddDbContext<FilesDbContext>((provider, options) =>
            options.UseSqliteConventional(provider.GetRequiredService<IOptions<FilesModuleOptions>>().Value.ConnectionString));
        services.AddScoped<IGreetingsBroker, ModuleGreetingsBroker>();
        services.AddHealthChecks().AddDatabaseReadinessCheck<FilesDbContext>();

        return services.AddOptions<FilesModuleOptions>()
            .Validate(options => !string.IsNullOrWhiteSpace(options.ConnectionString), "FilesModuleOptions.ConnectionString must be set.");
    }
}
