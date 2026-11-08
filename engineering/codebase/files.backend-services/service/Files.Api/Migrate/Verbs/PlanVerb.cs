using Files.Api.Migrate.Input;
using Files.Api.Migrate.Sources;

namespace Files.Api.Migrate.Verbs;

/// <summary>Runs <c>plan</c>: counts the source's files and bytes and touches nothing remote.</summary>
/// <param name="reader">The source's reader.</param>
public sealed class PlanVerb(ISourceReader reader)
{
    /// <summary>Counts the source.</summary>
    /// <param name="counters">The run's counters, filled with the totals.</param>
    /// <param name="cancellationToken">The token that cancels the count.</param>
    public async Task<int> RunAsync(MoverCounters counters, CancellationToken cancellationToken)
    {
        var totals = await reader.CountAsync(cancellationToken);
        counters.Files = totals.Files;
        counters.Bytes = totals.Bytes;
        return MigrateExitCodeConstants.Done;
    }
}
