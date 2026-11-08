using AwesomeAssertions;
using Files.Tests.Lanes;
using static Files.Tests.Integration.Files.ModuleHarness;

namespace Files.Tests.Integration.Files;

/// <summary>On a shared host a <c>sub</c> alone is not unique: Files stores the issuer beside the subject, and the acting app on the audit row.</summary>
/// <param name="lane">The lane whose store each test runs over.</param>
public abstract class ModuleIssuerTests(IFilesLane lane) : IAsyncLifetime
{
    private ModuleHarness harness = null!;

    public async Task InitializeAsync() => harness = await ModuleHarness.CreateAsync(lane);

    public async Task DisposeAsync() => await harness.DisposeAsync();

    [Fact]
    public async Task UploadAsync_ShouldStoreDifferentOwnerIssuers_WhenTwoCallersShareASubjectId()
    {
        Value(await harness.UploadAsAsync(Png, "a.png", "42", "https://idp-one.test"));
        Value(await harness.UploadAsAsync(Pdf, "b.pdf", "42", "https://idp-two.test"));
        Value(await harness.UploadAsAsync(Png.Concat(new byte[] { 9 }).ToArray(), "c.png", "42", issuer: null));

        var rows = await harness.RowsAsync("SELECT owner_id, owner_issuer FROM files_files ORDER BY seq");

        rows.Select(row => row["owner_id"]).Should().OnlyContain(id => (string?)id == "42");
        rows.Select(row => (string?)row["owner_issuer"]).Should().Equal("https://idp-one.test", "https://idp-two.test", null);
    }

    [Fact]
    public async Task UploadAsync_ShouldRecordActorAndIssuerOnTheAuditRow_WhenAnAppActsForAPerson()
    {
        Value(await harness.UploadAsAsync(Png, "a.png", "42", "https://idp-one.test", actorId: "app-gallery"));

        var rows = await harness.RowsAsync("SELECT subject_id, subject_issuer, actor_id FROM files_audit ORDER BY seq");

        rows.Should().ContainSingle().Which.Should().Match<IReadOnlyDictionary<string, object?>>(row =>
            (string?)row["subject_id"] == "42" && (string?)row["subject_issuer"] == "https://idp-one.test" && (string?)row["actor_id"] == "app-gallery");
    }
}

/// <summary>The tests over a SQLite file.</summary>
public sealed class ModuleIssuerSqliteTests() : ModuleIssuerTests(SqliteLane.Instance);

/// <summary>The tests over a PostgreSQL database.</summary>
/// <param name="lane">The assembly's container.</param>
[Collection(PostgresLane.Collection)]
public sealed class ModuleIssuerPostgresTests(PostgresLane lane) : ModuleIssuerTests(lane);
