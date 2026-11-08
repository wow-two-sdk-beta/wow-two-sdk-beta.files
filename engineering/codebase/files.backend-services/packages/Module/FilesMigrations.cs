using WoW.Two.Sdk.Beta.Be.Data.Migrations.Bespoke;

namespace WoW.Two.Sdk.Beta.Be.Files.Module;

/// <summary>Names the module's SQL migrations: the be sdk migration set <see cref="SetName"/>, embedded once per engine as
/// <c>Migrations/{Postgres,Sqlite}/NNN-name/{Apply,Rollback}.sql</c>.</summary>
/// <remarks>A host registers the set through <c>AddFilesMigrations</c>, which follows <see cref="FilesModuleOptions.Provider"/>;
/// a test or tool that registers it itself takes <see cref="CreateBroker"/>.</remarks>
public static class FilesMigrations
{
    /// <summary>Holds the set's name; its history lives in <c>files_migration_history</c>.</summary>
    public const string SetName = "files";

    /// <summary>Holds the resource folder of the PostgreSQL set.</summary>
    public const string PostgresResourcePrefix = "Migrations/Postgres/";

    /// <summary>Holds the resource folder of the SQLite set.</summary>
    public const string SqliteResourcePrefix = "Migrations/Sqlite/";

    /// <summary>Returns the resource folder of an engine's set.</summary>
    /// <param name="provider">The engine.</param>
    /// <returns>The folder's resource prefix.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The engine is neither PostgreSQL nor SQLite.</exception>
    public static string ResourcePrefix(DatabaseProvider provider) => provider switch
    {
        DatabaseProvider.Postgres => PostgresResourcePrefix,
        DatabaseProvider.Sqlite => SqliteResourcePrefix,
        _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Files runs on PostgreSQL or SQLite."),
    };

    /// <summary>Creates the source of an engine's set from the module's embedded SQL.</summary>
    /// <param name="provider">The engine.</param>
    /// <returns>The set's migration source.</returns>
    public static IMigrationBroker CreateBroker(DatabaseProvider provider) =>
        new EmbeddedResourceMigrationBroker(typeof(FilesMigrations).Assembly, ResourcePrefix(provider));
}
