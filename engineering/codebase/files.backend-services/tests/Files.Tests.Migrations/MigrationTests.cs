using AwesomeAssertions;
using Microsoft.Data.Sqlite;
using WoW.Two.Sdk.Beta.Be.Files.Module;
using WoW.Two.Sdk.Beta.Be.Data.Migrations.Bespoke;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;
using WoW.Two.Sdk.Beta.Be.Testing.Data.Migrations;

namespace Files.Tests.Migrations;

/// <summary>The module's embedded migrations over a throwaway SQLite file: apply, idempotency, and the Apply → Rollback → Apply round trip.</summary>
public sealed class MigrationTests : IDisposable
{
    private static readonly string[] Names = ["baseline", "files", "references", "audit", "greetings-removed", "owner-issuer", "imports", "variants"];

    private static readonly string[] Applied = ["001-baseline", "002-files", "003-references", "004-audit", "005-greetings-removed", "006-owner-issuer", "007-imports", "008-variants"];

    private static readonly string[] Tables = ["files_files", "files_blobs", "files_references", "files_audit", "files_imports", "files_import_sources", "files_import_items", "files_variants"];

    private static readonly string[] ImportTables = ["files_imports", "files_import_sources", "files_import_items"];

    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"files-migrations-{Guid.NewGuid():N}.db");

    /// <summary>Gets the connection string; pooling is off so the file is unlocked for teardown.</summary>
    private string ConnectionString => $"Data Source={databasePath};Pooling=False";

    public void Dispose()
    {
        if (File.Exists(databasePath))
            File.Delete(databasePath);
    }

    private MigratorHarness CreateMigrator(Action<MigrationOptions>? configure = null) =>
        MigratorHarness.CreateSqlite(ConnectionString, typeof(FilesModuleAssembly).Assembly, configure);

    [Fact]
    public async Task ApplyPending_ShouldCreateTheModuleSchemaAndBeIdempotent_WhenTheDatabaseIsFresh()
    {
        await using var migrator = CreateMigrator();

        var applied = Unwrap(await migrator.Runner.ApplyPendingAsync("startup", CancellationToken.None));

        applied.Should().Equal(Applied);
        foreach (var table in Tables)
            (await migrator.HasTableAsync(table)).Should().BeTrue(table);
        var history = await migrator.ReadHistoryAsync();
        history.Select(row => row.Ordinal).Should().Equal(1, 2, 3, 4, 5, 6, 7, 8);
        (await migrator.HasTableAsync("files_greeting_logs")).Should().BeFalse();
        history.Select(row => row.Name).Should().Equal(Names);
        history.Should().OnlyContain(row => row.AppliedBy == "startup" && row.Checksum.Length == 64);

        Unwrap(await migrator.Runner.ApplyPendingAsync("startup", CancellationToken.None)).Should().BeEmpty();
        (await migrator.ReadHistoryAsync()).Should().HaveCount(Applied.Length);
    }

    [Fact]
    public async Task ApplyPending_ShouldRestoreTheSchema_WhenEveryMigrationWasRolledBack()
    {
        await using var migrator = CreateMigrator(options => options.AllowRollback = true);
        Unwrap(await migrator.Runner.ApplyPendingAsync("test", CancellationToken.None));

        Unwrap(await migrator.Runner.RollbackAsync(targetOrdinal: 0, CancellationToken.None))
            .Should().Equal(Applied.Reverse());
        foreach (var table in Tables)
            (await migrator.HasTableAsync(table)).Should().BeFalse(table);
        (await migrator.ReadHistoryAsync()).Should().BeEmpty();

        Unwrap(await migrator.Runner.ApplyPendingAsync("test", CancellationToken.None)).Should().Equal(Applied);
        foreach (var table in Tables)
            (await migrator.HasTableAsync(table)).Should().BeTrue(table);
        var status = Unwrap(await migrator.Runner.GetStatusAsync(CancellationToken.None));
        status.Applied.Select(applied => applied.Ordinal).Should().Equal(1, 2, 3, 4, 5, 6, 7, 8);
        status.Pending.Should().BeEmpty();
        status.Drifted.Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyPending_ShouldAddTheIssuerAndActorColumns_AndRollbackShouldDropThem()
    {
        await using var migrator = CreateMigrator(options => options.AllowRollback = true);
        Unwrap(await migrator.Runner.ApplyPendingAsync("test", CancellationToken.None));

        Columns("files_files").Should().Contain("owner_issuer");
        Columns("files_audit").Should().Contain(["subject_issuer", "actor_id"]);

        Unwrap(await migrator.Runner.RollbackAsync(targetOrdinal: 5, CancellationToken.None)).Should().Equal("008-variants", "007-imports", "006-owner-issuer");
        Columns("files_files").Should().NotContain("owner_issuer").And.Contain("owner_id");
        Columns("files_audit").Should().NotContain(["subject_issuer", "actor_id"]).And.Contain("subject_id");
    }

    [Fact]
    public async Task ApplyPending_ShouldCreateTheImportTables_AndRollbackShouldDropThem()
    {
        await using var migrator = CreateMigrator(options => options.AllowRollback = true);
        Unwrap(await migrator.Runner.ApplyPendingAsync("test", CancellationToken.None));

        Columns("files_imports").Should().Equal("id", "tenant_id", "state", "created_at_utc");
        Columns("files_import_sources").Should().Equal("import_id", "source_id", "failed");
        Columns("files_import_items").Should().Contain(["import_id", "source_id", "source_key", "file_id", "sha256", "size", "reference", "created_at_utc"]);

        Unwrap(await migrator.Runner.RollbackAsync(targetOrdinal: 6, CancellationToken.None)).Should().Equal("008-variants", "007-imports");
        foreach (var table in ImportTables)
            (await migrator.HasTableAsync(table)).Should().BeFalse(table);
        (await migrator.HasTableAsync("files_files")).Should().BeTrue();

        Unwrap(await migrator.Runner.ApplyPendingAsync("test", CancellationToken.None)).Should().Equal("007-imports", "008-variants");
        foreach (var table in ImportTables)
            (await migrator.HasTableAsync(table)).Should().BeTrue(table);
    }

    [Fact]
    public async Task ApplyPending_ShouldCreateTheVariantsTable_AndRollbackShouldDropIt()
    {
        await using var migrator = CreateMigrator(options => options.AllowRollback = true);
        Unwrap(await migrator.Runner.ApplyPendingAsync("test", CancellationToken.None));

        Columns("files_variants").Should().Equal("tenant_id", "file_id", "width", "height", "content_type", "size", "sha256", "created_at_utc");

        Unwrap(await migrator.Runner.RollbackAsync(targetOrdinal: 7, CancellationToken.None)).Should().Equal("008-variants");
        (await migrator.HasTableAsync("files_variants")).Should().BeFalse();
        (await migrator.HasTableAsync("files_files")).Should().BeTrue();
        (await migrator.HasTableAsync("files_blobs")).Should().BeTrue();

        Unwrap(await migrator.Runner.ApplyPendingAsync("test", CancellationToken.None)).Should().Equal("008-variants");
        (await migrator.HasTableAsync("files_variants")).Should().BeTrue();
    }

    private List<string> Columns(string table)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT name FROM pragma_table_info('{table}')";
        using var reader = command.ExecuteReader();
        var names = new List<string>();
        while (reader.Read())
            names.Add(reader.GetString(0));
        return names;
    }

    /// <summary>Returns a migrator result's value, failing the test with the migrator's own message otherwise.</summary>
    private static T Unwrap<T>(Result<T> result) where T : notnull =>
        result is Result<T>.Success success
            ? success.Value
            : throw new Xunit.Sdk.XunitException(((Result<T>.Failure)result).Error.Message);
}
