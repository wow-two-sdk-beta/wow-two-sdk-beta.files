using AwesomeAssertions;
using Files.Tests.Lanes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WoW.Two.Sdk.Beta.Be.Data;
using WoW.Two.Sdk.Beta.Be.Files.Module;
using WoW.Two.Sdk.Beta.Be.Files.Module.Persistence;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;
using WoW.Two.Sdk.Beta.Be.Storage.Core;
using WoW.Two.Sdk.Beta.Be.Storage.FileSystem;

namespace Files.Tests.Integration.Files;

/// <summary>A host registers the schema as the service does — <c>AddFiles</c> plus <c>AddFilesMigrations</c> — and the be
/// sdk's deploy step creates the missing store and applies the set of the module's engine, which the context then reads.</summary>
/// <param name="lane">The lane whose store the host points at.</param>
public abstract class FilesMigrationsRegistrationTests(IFilesLane lane) : IAsyncLifetime
{
    private static readonly string[] Applied = ["001-baseline", "002-files", "003-references", "004-audit", "005-greetings-removed", "006-owner-issuer", "007-imports", "008-variants"];

    private readonly string root = Path.Combine(Path.GetTempPath(), $"files-registration-{Guid.NewGuid():N}");
    private FilesTestStore store = null!;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(root);
        store = await lane.CreateAsync(root);
    }

    public async Task DisposeAsync()
    {
        await store.DisposeAsync();
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public async Task MigrateBespokeForDeployAsync_ShouldCreateTheStoreAndApplyTheFilesSet_ForTheModulesEngine()
    {
        // Drop the lane's empty store, so the deploy step has to create it.
        await store.DisposeAsync();
        var services = new ServiceCollection().AddLogging();
        services.AddSingleton<IBlobRepository>(new LocalFileBlobRepository(Path.Combine(root, "blobs")));
        services.AddFiles().Configure(options =>
        {
            store.ApplyTo(options);
            options.SweepInterval = TimeSpan.Zero;
        });
        services.AddFilesMigrations();
        await using var provider = services.BuildServiceProvider(validateScopes: true);

        var applied = (await provider.MigrateBespokeForDeployAsync()).ValueOrThrow();

        applied.Should().ContainSingle().Which.Should().BeEquivalentTo(new { Set = FilesMigrations.SetName, Applied });
        (await store.RowsAsync("SELECT ordinal FROM files_migration_history ORDER BY ordinal")).Should().HaveCount(Applied.Length);
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FilesDbContext>();
        (await db.Files.CountAsync(file => file.State == FileState.Ready && file.Kind == FileKind.Image)).Should().Be(0);
        (await provider.MigrateBespokeForDeployAsync()).ValueOrThrow().Should().ContainSingle().Which.Applied.Should().BeEmpty();
    }
}

/// <summary>The registration over a SQLite file.</summary>
public sealed class FilesMigrationsRegistrationSqliteTests() : FilesMigrationsRegistrationTests(SqliteLane.Instance);

/// <summary>The registration over a PostgreSQL database.</summary>
/// <param name="lane">The assembly's container.</param>
[Collection(PostgresLane.Collection)]
public sealed class FilesMigrationsRegistrationPostgresTests(PostgresLane lane) : FilesMigrationsRegistrationTests(lane);
