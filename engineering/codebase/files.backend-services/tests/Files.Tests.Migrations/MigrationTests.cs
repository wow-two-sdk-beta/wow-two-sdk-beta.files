using AwesomeAssertions;
using Files.Tests.Lanes;
using WoW.Two.Sdk.Beta.Be.Data.Migrations.Bespoke;
using WoW.Two.Sdk.Beta.Be.Files.Audit;
using WoW.Two.Sdk.Beta.Be.Files.Module;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Foundation.Naming;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;
using WoW.Two.Sdk.Beta.Be.Testing.Data.Migrations;
using WoW.Two.Sdk.Beta.Be.Testing.Data.Migrations.Lint;

namespace Files.Tests.Migrations;

/// <summary>The module's set <c>files</c> on one engine's store: apply, idempotency, and the Apply → Rollback → Apply round
/// trip, once per lane.</summary>
/// <param name="lane">The lane whose fresh store each test migrates.</param>
public abstract class MigrationTests(IFilesLane lane) : IAsyncLifetime
{
    private static readonly string[] Names = ["baseline", "files", "references", "audit", "greetings-removed", "owner-issuer", "imports", "variants"];

    private static readonly string[] Applied = ["001-baseline", "002-files", "003-references", "004-audit", "005-greetings-removed", "006-owner-issuer", "007-imports", "008-variants"];

    private static readonly string[] Tables = ["files_files", "files_blobs", "files_references", "files_audit", "files_imports", "files_import_sources", "files_import_items", "files_variants"];

    private static readonly string[] ImportTables = ["files_imports", "files_import_sources", "files_import_items"];

    private readonly string folder = Path.Combine(Path.GetTempPath(), $"files-migrations-{Guid.NewGuid():N}");

    /// <summary>Gets the test's fresh store.</summary>
    protected FilesTestStore Store { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(folder);
        Store = await lane.CreateAsync(folder);
    }

    public async Task DisposeAsync()
    {
        await Store.DisposeAsync();
        Directory.Delete(folder, recursive: true);
    }

    [Fact]
    public async Task ApplyPending_ShouldCreateTheModuleSchemaAndBeIdempotent_WhenTheDatabaseIsFresh()
    {
        await using var migrator = Store.CreateMigrator();
        var runner = migrator.RunnerFor(FilesMigrations.SetName);

        var applied = Unwrap(await runner.ApplyPendingAsync("startup", CancellationToken.None));

        applied.Should().Equal(Applied);
        foreach (var table in Tables)
            (await migrator.HasTableAsync(table)).Should().BeTrue(table);
        var history = await migrator.ReadHistoryAsync(FilesMigrations.SetName);
        history.Select(row => row.Ordinal).Should().Equal(1, 2, 3, 4, 5, 6, 7, 8);
        (await migrator.HasTableAsync("files_greeting_logs")).Should().BeFalse();
        history.Select(row => row.Name).Should().Equal(Names);
        history.Should().OnlyContain(row => row.AppliedBy == "startup" && row.Checksum.Length == 64);

        Unwrap(await runner.ApplyPendingAsync("startup", CancellationToken.None)).Should().BeEmpty();
        (await migrator.ReadHistoryAsync(FilesMigrations.SetName)).Should().HaveCount(Applied.Length);
    }

    [Fact]
    public async Task ApplyPending_ShouldRestoreTheSchema_WhenEveryMigrationWasRolledBack()
    {
        await using var migrator = Store.CreateMigrator(options => options.AllowRollback = true);
        var runner = migrator.RunnerFor(FilesMigrations.SetName);
        Unwrap(await runner.ApplyPendingAsync("test", CancellationToken.None));

        Unwrap(await runner.RollbackAsync(targetOrdinal: 0, CancellationToken.None))
            .Should().Equal(Applied.Reverse());
        foreach (var table in Tables)
            (await migrator.HasTableAsync(table)).Should().BeFalse(table);
        (await migrator.ReadHistoryAsync(FilesMigrations.SetName)).Should().BeEmpty();

        Unwrap(await runner.ApplyPendingAsync("test", CancellationToken.None)).Should().Equal(Applied);
        foreach (var table in Tables)
            (await migrator.HasTableAsync(table)).Should().BeTrue(table);
        var status = Unwrap(await runner.GetStatusAsync(CancellationToken.None));
        status.Applied.Select(applied => applied.Ordinal).Should().Equal(1, 2, 3, 4, 5, 6, 7, 8);
        status.Pending.Should().BeEmpty();
        status.Drifted.Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyPending_ShouldAddTheIssuerAndActorColumns_AndRollbackShouldDropThem()
    {
        await using var migrator = Store.CreateMigrator(options => options.AllowRollback = true);
        var runner = migrator.RunnerFor(FilesMigrations.SetName);
        Unwrap(await runner.ApplyPendingAsync("test", CancellationToken.None));

        (await Store.ColumnsAsync("files_files")).Should().Contain("owner_issuer");
        (await Store.ColumnsAsync("files_audit")).Should().Contain(["subject_issuer", "actor_id"]);

        Unwrap(await runner.RollbackAsync(targetOrdinal: 5, CancellationToken.None)).Should().Equal("008-variants", "007-imports", "006-owner-issuer");
        (await Store.ColumnsAsync("files_files")).Should().NotContain("owner_issuer").And.Contain("owner_id");
        (await Store.ColumnsAsync("files_audit")).Should().NotContain(["subject_issuer", "actor_id"]).And.Contain("subject_id");
    }

    [Fact]
    public async Task ApplyPending_ShouldCreateTheImportTables_AndRollbackShouldDropThem()
    {
        await using var migrator = Store.CreateMigrator(options => options.AllowRollback = true);
        var runner = migrator.RunnerFor(FilesMigrations.SetName);
        Unwrap(await runner.ApplyPendingAsync("test", CancellationToken.None));

        (await Store.ColumnsAsync("files_imports")).Should().Equal("id", "tenant_id", "state", Store.At("created_at"));
        (await Store.ColumnsAsync("files_import_sources")).Should().Equal("import_id", "source_id", "failed");
        (await Store.ColumnsAsync("files_import_items")).Should().Contain(["import_id", "source_id", "source_key", "file_id", "sha256", "size", "reference", Store.At("created_at")]);

        Unwrap(await runner.RollbackAsync(targetOrdinal: 6, CancellationToken.None)).Should().Equal("008-variants", "007-imports");
        foreach (var table in ImportTables)
            (await migrator.HasTableAsync(table)).Should().BeFalse(table);
        (await migrator.HasTableAsync("files_files")).Should().BeTrue();

        Unwrap(await runner.ApplyPendingAsync("test", CancellationToken.None)).Should().Equal("007-imports", "008-variants");
        foreach (var table in ImportTables)
            (await migrator.HasTableAsync(table)).Should().BeTrue(table);
    }

    [Fact]
    public async Task ApplyPending_ShouldCreateTheVariantsTable_AndRollbackShouldDropIt()
    {
        await using var migrator = Store.CreateMigrator(options => options.AllowRollback = true);
        var runner = migrator.RunnerFor(FilesMigrations.SetName);
        Unwrap(await runner.ApplyPendingAsync("test", CancellationToken.None));

        (await Store.ColumnsAsync("files_variants")).Should().Equal("tenant_id", "file_id", "width", "height", "content_type", "size", "sha256", Store.At("created_at"));

        Unwrap(await runner.RollbackAsync(targetOrdinal: 7, CancellationToken.None)).Should().Equal("008-variants");
        (await migrator.HasTableAsync("files_variants")).Should().BeFalse();
        (await migrator.HasTableAsync("files_files")).Should().BeTrue();
        (await migrator.HasTableAsync("files_blobs")).Should().BeTrue();

        Unwrap(await runner.ApplyPendingAsync("test", CancellationToken.None)).Should().Equal("008-variants");
        (await migrator.HasTableAsync("files_variants")).Should().BeTrue();
    }

    /// <summary>Returns a migrator result's value, failing the test with the migrator's own message otherwise.</summary>
    protected static T Unwrap<T>(Result<T> result) where T : notnull =>
        result is Result<T>.Success success
            ? success.Value
            : throw new Xunit.Sdk.XunitException(((Result<T>.Failure)result).Error.Message);
}

/// <summary>The set from <c>Migrations/Sqlite/</c> over a throwaway SQLite file.</summary>
public sealed class SqliteMigrationTests() : MigrationTests(SqliteLane.Instance);

/// <summary>The set from <c>Migrations/Postgres/</c> over a fresh PostgreSQL database, plus what only PostgreSQL holds:
/// rerunnable scripts, native enums and column types.</summary>
/// <param name="lane">The assembly's container.</param>
[Collection(PostgresLane.Collection)]
public sealed class PostgresMigrationTests(PostgresLane lane) : MigrationTests(lane)
{
    [Fact]
    public async Task Scripts_ShouldBeIdempotent_EveryFile()
    {
        await using var migrator = Store.CreateMigrator(options => options.AllowRollback = true);

        await migrator.AssertIdempotentAsync(FilesMigrations.SetName);
    }

    [Fact]
    public async Task Lint_ShouldFindNothing_AfterTheFirstSet()
    {
        await using var migrator = Store.CreateMigrator();

        // 001–008 arrived together as the PostgreSQL set's first release, applied only to empty databases, so 003's index
        // on files_files never locks a populated table; every later file is linted for lock risk and must be an expand.
        var findings = await MigrationLint.CheckAsync(migrator.Services, lint => lint
            .LintAfter(FilesMigrations.SetName, 8)
            .ExpandOnlyAfter(FilesMigrations.SetName, 8));

        findings.Should().BeEmpty();
    }

    [Fact]
    public async Task EnumTypes_ShouldLabelEveryClrMember_InSnakeCase()
    {
        await Store.MigrateAsync();

        (await LabelsAsync("files_file_kind")).Should().Equal(Expected<FileKind>());
        (await LabelsAsync("files_file_visibility")).Should().Equal(Expected<FileVisibility>());
        (await LabelsAsync("files_file_state")).Should().Equal(Expected<FileState>());
        (await LabelsAsync("files_file_audit_action")).Should().Equal(Expected<FileAuditAction>());
        (await LabelsAsync("files_file_audit_outcome")).Should().Equal(Expected<FileAuditOutcome>());
    }

    [Fact]
    public async Task Columns_ShouldTakeThePostgresTypes_UuidIdsAndTimestamptzInstants()
    {
        await Store.MigrateAsync();

        var columns = await Store.RowsAsync(
            "SELECT table_name, column_name, data_type FROM information_schema.columns WHERE table_schema = current_schema() AND table_name LIKE 'files\\_%' AND table_name NOT LIKE '%migration%' AND table_name NOT LIKE '%seed%'");

        columns.Should().NotBeEmpty();
        columns.Where(column => (string)column["column_name"]! is "id" or "file_id" or "import_id")
            .Should().OnlyContain(column => (string)column["data_type"]! == "uuid");
        columns.Where(column => ((string)column["column_name"]!).EndsWith("_at", StringComparison.Ordinal))
            .Should().HaveCount(12).And.OnlyContain(column => (string)column["data_type"]! == "timestamp with time zone");
        columns.Should().NotContain(column => ((string)column["column_name"]!).EndsWith("_utc", StringComparison.Ordinal));
    }

    private async Task<IReadOnlyList<string>> LabelsAsync(string type) =>
        (await Store.RowsAsync($"SELECT unnest(enum_range(NULL::{type}))::text AS label")).Select(row => (string)row["label"]!).ToList();

    private static IEnumerable<string> Expected<TEnum>() where TEnum : struct, Enum =>
        Enum.GetNames<TEnum>().Select(name => CaseMapper.ToCase(name, CaseStyle.Snake));
}
