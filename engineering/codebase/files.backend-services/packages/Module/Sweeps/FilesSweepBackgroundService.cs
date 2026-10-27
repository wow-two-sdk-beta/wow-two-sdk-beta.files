using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Sweeps;

/// <summary>Runs the sweep every <see cref="FilesModuleOptions.SweepInterval"/> for as long as the host lives; a zero
/// interval leaves it idle.</summary>
internal sealed class FilesSweepBackgroundService(
    IServiceScopeFactory scopes,
    IOptions<FilesModuleOptions> options,
    TimeProvider time,
    ILogger<FilesSweepBackgroundService> logger) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = options.Value.SweepInterval;
        if (interval <= TimeSpan.Zero)
            return;

        using var timer = new PeriodicTimer(interval, time);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var result = await scope.ServiceProvider.GetRequiredService<IFilesSweepService>().SweepAsync(stoppingToken);
                if (result.Files > 0 || result.Blobs > 0)
                    logger.LogInformation("Files sweep purged {Files} files and {Blobs} blobs.", result.Files, result.Blobs);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Files sweep failed; the next tick retries.");
            }
        }
    }
}
