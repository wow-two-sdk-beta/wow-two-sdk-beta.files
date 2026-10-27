using WoW.Two.Sdk.Beta.Be.Data;
using WoW.Two.Sdk.Beta.Be.Files.Server.Delivery;
using WoW.Two.Sdk.Beta.Be.Meta;

namespace Files.Api.Configurations;

/// <summary>Extends the host builder and the web application for startup wiring.</summary>
public static partial class HostConfiguration
{
    /// <summary>Configures the application builder (services).</summary>
    /// <param name="builder">The web application builder to configure.</param>
    public static WebApplicationBuilder Configure(this WebApplicationBuilder builder)
    {
        // Lay the SDK boot floor before any domain seam. It reads the settings file a target mounts
        // (appsettings.Target.json) and, on a local run, this machine's (appsettings.Local.json). The hosted pages and
        // the console are served from the same host.
        builder.AddApiDefaults(options =>
        {
            options.ServiceName = "files";
            options.EnableOtlpExporters = false;
            options.EnableHttpsRedirection = !builder.Environment.IsDevelopment();
            options.SpaHosting = _ => { };
            options.TrustProxiesFrom(builder.Configuration.GetSection("Deployment"));
        });

        return builder
            .AddFilesModule()
            .AddControllers();
    }

    /// <summary>Runs startup tasks, then configures middleware and endpoints.</summary>
    /// <param name="app">The built web application to configure.</param>
    public static async Task<WebApplication> ConfigureAsync(this WebApplication app)
    {
        // Create the database file if missing and apply the module's pending SQL migrations before serving. A deployed
        // host applies at boot too until its deploy runs a migrate step (core logs event 3014 as a reminder).
        await app.Services.MigrateBespokeOnStartupAsync(migrations => migrations.ApplyWhenDeployed = true, app.Lifetime.ApplicationStopping);

        // SDK pipeline: secure headers, compression, the SPA bundle, /health (database readiness) and the SPA fallback.
        app.UseApiDefaults();
        app.MapControllers();
        app.MapFilesDelivery();

        return app;
    }
}
