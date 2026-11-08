using Files.Api.Migrate.Api;
using Files.Api.Migrate.Input;
using Files.Api.Migrate.Sources;

namespace Files.Api.Migrate.Verbs;

/// <summary>Runs <c>push</c>: reads the import's map once, skips keys already there, and uploads the rest with their
/// SHA-256, hashed before each upload starts.</summary>
/// <remarks>A <c>409</c>, <c>422</c> or exhausted retry counts as failed and the run goes on; a refusal stops it.</remarks>
/// <param name="reader">The source's reader.</param>
/// <param name="client">The import's client.</param>
/// <param name="source">The source.</param>
/// <param name="output">The run's output.</param>
/// <param name="options">The buffering limits.</param>
public sealed class PushVerb(ISourceReader reader, ImportApiClient client, MigrateSource source, MoverOutput output, MigrateOptions options)
{
    /// <summary>Pushes the source.</summary>
    /// <param name="counters">The run's counters.</param>
    /// <param name="cancellationToken">The token that cancels the run.</param>
    /// <exception cref="MigrateRefusedException">Files refused the token or does not know the import.</exception>
    /// <exception cref="MigrateUnavailableException">The map stayed unreachable.</exception>
    public async Task<int> RunAsync(MoverCounters counters, CancellationToken cancellationToken)
    {
        var map = await client.ReadMapAsync(source.Id, cancellationToken);
        await foreach (var item in reader.ReadAsync(cancellationToken))
        {
            counters.Files++;
            if (map.TryGetValue(item.Key, out var known))
            {
                counters.Skipped++;
                counters.Bytes += known.Size;
                output.Progress(counters);
                continue;
            }

            await using var content = await ContentBuffer.FillAsync(item, options.MemoryLimit, options.TempDirectory, cancellationToken);
            counters.Bytes += content.Length;
            var upload = new ImportUpload(source.Id, item.Key, content.Sha256, source.Reference?.Render(item.Values, content.Sha256), item.Name, item.ContentType);
            var answer = await client.UploadAsync(upload, content, cancellationToken);
            switch (answer.Outcome)
            {
                case ImportUploadOutcome.Skipped:
                    counters.Skipped++;
                    break;
                case ImportUploadOutcome.Failed:
                    counters.Failed++;
                    output.Failed(item.Key, answer.Reason);
                    break;
                case ImportUploadOutcome.Refused:
                    throw new MigrateRefusedException($"Files refused the upload of '{item.Key}' with {answer.Reason}.");
            }

            output.Progress(counters);
        }

        return counters.Failed > 0 ? MigrateExitCodeConstants.Failed : MigrateExitCodeConstants.Done;
    }
}
