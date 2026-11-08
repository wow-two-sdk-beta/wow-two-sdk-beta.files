using AwesomeAssertions;
using Files.Tests.Lanes;
using WoW.Two.Sdk.Beta.Be.Files.Module.Sweeps;
using static Files.Tests.Integration.Files.ModuleHarness;

namespace Files.Tests.Integration.Files;

/// <summary>The sweep purges deleted files past retention and expired files, then bytes no file uses after the grace.</summary>
/// <param name="lane">The lane whose store each test runs over.</param>
public abstract class FilesSweepTests(IFilesLane lane) : IAsyncLifetime
{
    private ModuleHarness harness = null!;

    public async Task InitializeAsync() => harness = await CreateAsync(lane, options =>
        options.Policies["brief"] = new() { DeletedRetention = TimeSpan.FromMinutes(1) });

    public async Task DisposeAsync() => await harness.DisposeAsync();

    [Fact]
    public async Task SweepAsync_ShouldPurgeTheFileAndItsBytes_WhenTheRetentionAndTheGraceHavePassed()
    {
        var file = Value(await harness.UploadAsync(Png, "a.png"));
        Value(await harness.StoreAsync(store => store.DeleteAsync(file.Id)));

        harness.Clock.Advance(TimeSpan.FromDays(31));
        var result = await SweepAsync();

        result.Should().Be(new FileSweepResultModel(1, 1));
        harness.BlobFiles().Should().BeEmpty();
    }

    [Fact]
    public async Task SweepAsync_ShouldKeepTheBytes_WhileAnotherFileUsesThem()
    {
        var deleted = Value(await harness.UploadAsync(Png, "a.png", appId: "brief"));
        Value(await harness.UploadAsync(Png, "b.png"));
        Value(await harness.StoreAsync(store => store.DeleteAsync(deleted.Id)));

        harness.Clock.Advance(TimeSpan.FromDays(1));

        (await SweepAsync()).Should().Be(new FileSweepResultModel(1, 0));
        harness.BlobFiles().Should().ContainSingle();
    }

    [Fact]
    public async Task SweepAsync_ShouldKeepUnusedBytes_UntilTheGraceHasPassed()
    {
        var file = Value(await harness.UploadAsync(Png, "a.png", appId: "brief"));
        Value(await harness.StoreAsync(store => store.DeleteAsync(file.Id)));

        harness.Clock.Advance(TimeSpan.FromMinutes(2));
        (await SweepAsync()).Should().Be(new FileSweepResultModel(1, 0));

        harness.Clock.Advance(TimeSpan.FromHours(1));
        (await SweepAsync()).Should().Be(new FileSweepResultModel(0, 1));
    }

    [Fact]
    public async Task SweepAsync_ShouldPurgeExpiredFiles_WhenTheirExpiryHasPassed()
    {
        Value(await harness.UploadAsync(Pdf, "a.pdf", expiresAtUtc: harness.Clock.GetUtcNow().AddHours(1)));
        Value(await harness.UploadAsync(Png, "b.png"));

        harness.Clock.Advance(TimeSpan.FromHours(3));

        (await SweepAsync()).Should().Be(new FileSweepResultModel(1, 1));
        harness.BlobFiles().Should().ContainSingle();
    }

    private Task<FileSweepResultModel> SweepAsync() =>
        harness.WithAsync<IFilesSweepService, FileSweepResultModel>(sweep => sweep.SweepAsync());
}

/// <summary>The tests over a SQLite file.</summary>
public sealed class FilesSweepSqliteTests() : FilesSweepTests(SqliteLane.Instance);

/// <summary>The tests over a PostgreSQL database.</summary>
/// <param name="lane">The assembly's container.</param>
[Collection(PostgresLane.Collection)]
public sealed class FilesSweepPostgresTests(PostgresLane lane) : FilesSweepTests(lane);
