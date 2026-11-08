namespace Files.Api.Migrate.Input;

/// <summary>Holds the mover's command and verb names.</summary>
public static class MigrateVerbConstants
{
    /// <summary>Holds the first argument that runs the mover instead of the web host.</summary>
    public const string Command = "migrate";

    /// <summary>Holds the verb that counts files and bytes and touches nothing remote.</summary>
    public const string Plan = "plan";

    /// <summary>Holds the verb that uploads the files the import's map does not hold yet.</summary>
    public const string Push = "push";

    /// <summary>Holds the verb that compares each source item's SHA-256 with the import's map.</summary>
    public const string Verify = "verify";
}

/// <summary>Holds the source kinds the mover reads.</summary>
public static class MigrateSourceKindConstants
{
    /// <summary>Holds the kind that reads every file under <c>{root}/{path}</c>.</summary>
    public const string VolumeFolder = "volume-folder";

    /// <summary>Holds the kind that reads a column of a read-only SQLite file under the root.</summary>
    public const string SqliteColumn = "sqlite-column";

    /// <summary>Holds the kind that reads a <c>bytea</c> column over a PostgreSQL connection.</summary>
    public const string PostgresColumn = "postgres-column";
}

/// <summary>Holds the mover's exit codes.</summary>
public static class MigrateExitCodeConstants
{
    /// <summary>Holds the code of a run that finished with nothing failed.</summary>
    public const int Done = 0;

    /// <summary>Holds the code of refused input or a refused token; never retried.</summary>
    public const int Refused = 1;

    /// <summary>Holds the code of a run in which something failed; a rerun resumes.</summary>
    public const int Failed = 2;
}
