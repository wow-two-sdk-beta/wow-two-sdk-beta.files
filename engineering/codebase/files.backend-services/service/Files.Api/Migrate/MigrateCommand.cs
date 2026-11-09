using System.Data.Common;
using System.Globalization;
using Files.Api.Migrate.Api;
using Files.Api.Migrate.Input;
using Files.Api.Migrate.Sources;
using Files.Api.Migrate.Verbs;
using Microsoft.Data.Sqlite;

namespace Files.Api.Migrate;

/// <summary>Runs <c>Files.Api migrate plan|push|verify</c> (<c>product/features/imports.md</c> § Mover): one source per
/// run, one JSON document on standard input, no settings file and no web host.</summary>
/// <remarks>Exits <c>0</c> done, <c>1</c> on refused input or a refused token, <c>2</c> when something failed and a rerun
/// resumes. Every run that gets past its input ends with the <c>{"result":…}</c> line.</remarks>
public static class MigrateCommand
{
    /// <summary>Runs one verb.</summary>
    /// <param name="args">The arguments after <c>migrate</c>: the verb alone.</param>
    /// <param name="stdin">Standard input.</param>
    /// <param name="stdout">Standard output.</param>
    /// <param name="stderr">Standard error.</param>
    /// <param name="options">The mover's limits; <see langword="null"/> takes the defaults.</param>
    /// <param name="cancellationToken">The token that cancels the run.</param>
    /// <returns>The exit code, one of <see cref="MigrateExitCodeConstants"/>.</returns>
    public static async Task<int> RunAsync(
        IReadOnlyList<string> args,
        TextReader stdin,
        TextWriter stdout,
        TextWriter stderr,
        MigrateOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new MigrateOptions();
        if (args is not [MigrateVerbConstants.Plan or MigrateVerbConstants.Push or MigrateVerbConstants.Verify])
        {
            await stderr.WriteLineAsync("Usage: migrate plan|push|verify, with one JSON document on standard input.");
            return MigrateExitCodeConstants.Refused;
        }

        var verb = args[0];
        MigrateRequest request;
        ISourceReader reader;
        try
        {
            request = MigrateRequestParser.Parse(await stdin.ReadToEndAsync(cancellationToken), verb);
            reader = SourceReaderFactory.Create(request);
            if (reader is VolumeFolderSourceReader { Exists: false })
                await stderr.WriteLineAsync("The source folder 'root/source.path' does not exist; it reads as an empty source.");
        }
        catch (MigrateInputException exception)
        {
            await stderr.WriteLineAsync(exception.Message);
            return MigrateExitCodeConstants.Refused;
        }

        var output = new MoverOutput(stdout, stderr, options.Time, options.ProgressInterval);
        var counters = new MoverCounters();
        using var http = verb == MigrateVerbConstants.Plan ? null : CreateHttpClient(request.Files!, options);
        try
        {
            return verb switch
            {
                MigrateVerbConstants.Plan => await new PlanVerb(reader).RunAsync(counters, cancellationToken),
                MigrateVerbConstants.Push => await new PushVerb(reader, Client(http!, request, options), request.Source, output, options).RunAsync(counters, cancellationToken),
                _ => await new VerifyVerb(reader, Client(http!, request, options), request.Source, output).RunAsync(counters, cancellationToken),
            };
        }
        catch (Exception exception) when (exception is MigrateInputException or MigrateRefusedException)
        {
            output.Error(exception.Message);
            return MigrateExitCodeConstants.Refused;
        }
        catch (MigrateUnavailableException exception)
        {
            output.Error(exception.Message);
            return MigrateExitCodeConstants.Failed;
        }
        catch (DbException exception)
        {
            output.Error(exception is SqliteException sqlite
                ? $"The source could not be read: {sqlite.Message}"
                : string.Create(CultureInfo.InvariantCulture, $"The source could not be read ({exception.GetType().Name} {exception.SqlState})."));
            return IsRefusedQuery(exception) ? MigrateExitCodeConstants.Refused : MigrateExitCodeConstants.Failed;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            output.Error($"The source could not be read ({exception.GetType().Name}).");
            return MigrateExitCodeConstants.Failed;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            output.Error("The run was cancelled.");
            return MigrateExitCodeConstants.Failed;
        }
        finally
        {
            output.Result(verb, counters);
            (reader as IDisposable)?.Dispose();
        }
    }

    /// <summary>Tells a query the source refuses for good — a missing table or column, a denied login — from a failure a
    /// rerun may get past.</summary>
    /// <param name="exception">The database's error.</param>
    public static bool IsRefusedQuery(DbException exception) => exception switch
    {
        SqliteException sqlite => sqlite.SqliteErrorCode == 1,
        _ => exception.SqlState is { Length: 5 } state && (state.StartsWith("42", StringComparison.Ordinal) || state.StartsWith("28", StringComparison.Ordinal)),
    };

    private static ImportApiClient Client(HttpClient http, MigrateRequest request, MigrateOptions options) =>
        new(http, request.Import!, request.Token!, options.RetryDelays, options.Time, options.MapPageSize);

    private static HttpClient CreateHttpClient(Uri files, MigrateOptions options)
    {
        var handler = options.Handler ?? new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5) };
        return new HttpClient(handler, disposeHandler: options.Handler is null)
        {
            BaseAddress = files,
            Timeout = options.RequestTimeout,
        };
    }
}
