using System.Globalization;

namespace Files.Api.Migrate.Verbs;

/// <summary>Counts a run: <see cref="Files"/> and <see cref="Bytes"/> are every source item the verb went through, of
/// which <see cref="Skipped"/> were already in Files and <see cref="Failed"/> failed.</summary>
public sealed class MoverCounters
{
    /// <summary>Gets or sets the number of items gone through.</summary>
    public long Files { get; set; }

    /// <summary>Gets or sets the sum of their sizes.</summary>
    public long Bytes { get; set; }

    /// <summary>Gets or sets the number of items already in Files.</summary>
    public long Skipped { get; set; }

    /// <summary>Gets or sets the number of items that failed.</summary>
    public long Failed { get; set; }
}

/// <summary>Writes the mover's output: JSON lines on standard output — <c>{"progress":…}</c> at most once per interval,
/// <c>{"result":…}</c> last — and counts and source keys on standard error, never contents or a secret.</summary>
/// <param name="stdout">Standard output.</param>
/// <param name="stderr">Standard error.</param>
/// <param name="time">The clock the interval runs on.</param>
/// <param name="interval">The shortest time between two progress lines.</param>
public sealed class MoverOutput(TextWriter stdout, TextWriter stderr, TimeProvider time, TimeSpan interval)
{
    private long last = time.GetTimestamp();

    /// <summary>Writes a progress line when the interval has passed since the run started or the last one.</summary>
    /// <param name="counters">The run's counters so far.</param>
    public void Progress(MoverCounters counters)
    {
        if (time.GetElapsedTime(last) < interval)
            return;

        last = time.GetTimestamp();
        stdout.WriteLine(string.Create(CultureInfo.InvariantCulture, $$$"""{"progress":{"files":{{{counters.Files}}},"bytes":{{{counters.Bytes}}}}}"""));
        stdout.Flush();
    }

    /// <summary>Writes the result line and the counts.</summary>
    /// <param name="verb">The verb that ran.</param>
    /// <param name="counters">The run's counters.</param>
    public void Result(string verb, MoverCounters counters)
    {
        stdout.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $$$"""{"result":{"files":{{{counters.Files}}},"bytes":{{{counters.Bytes}}},"skipped":{{{counters.Skipped}}},"failed":{{{counters.Failed}}}}}"""));
        stdout.Flush();
        stderr.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"{verb}: files={counters.Files} bytes={counters.Bytes} skipped={counters.Skipped} failed={counters.Failed}"));
    }

    /// <summary>Writes one failed key and its reason (a status code, <c>missing</c> or <c>different</c>).</summary>
    /// <param name="key">The source key.</param>
    /// <param name="reason">Why it failed.</param>
    public void Failed(string key, string reason) => stderr.WriteLine($"failed {key}: {reason}");

    /// <summary>Writes why a run stopped.</summary>
    /// <param name="message">A message naming fields, kinds or status codes only.</param>
    public void Error(string message) => stderr.WriteLine(message);
}
