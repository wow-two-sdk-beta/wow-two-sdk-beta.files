using Files.Api.Migrate.Api;
using Files.Api.Migrate.Input;
using Files.Api.Migrate.Sources;

namespace Files.Api.Migrate.Verbs;

/// <summary>Runs <c>verify</c>: pages the import's map, recomputes each source item's SHA-256 and fails every key the
/// map is missing or holds with another hash.</summary>
/// <param name="reader">The source's reader.</param>
/// <param name="client">The import's client.</param>
/// <param name="source">The source.</param>
/// <param name="output">The run's output.</param>
public sealed class VerifyVerb(ISourceReader reader, ImportApiClient client, MigrateSource source, MoverOutput output)
{
    /// <summary>Verifies the source against the map.</summary>
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
            string sha256;
            await using (var content = item.OpenContent())
            {
                (sha256, var length) = await ContentBuffer.HashAsync(content, cancellationToken);
                counters.Bytes += length;
            }

            if (!map.TryGetValue(item.Key, out var known))
            {
                counters.Failed++;
                output.Failed(item.Key, "missing");
            }
            else if (!string.Equals(known.Sha256, sha256, StringComparison.Ordinal))
            {
                counters.Failed++;
                output.Failed(item.Key, "different");
            }

            output.Progress(counters);
        }

        return counters.Failed > 0 ? MigrateExitCodeConstants.Failed : MigrateExitCodeConstants.Done;
    }
}
