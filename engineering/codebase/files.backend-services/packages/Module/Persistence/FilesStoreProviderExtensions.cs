using Microsoft.EntityFrameworkCore;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using WoW.Two.Sdk.Beta.Be.Data.EntityFrameworkCore.Postgres;
using WoW.Two.Sdk.Beta.Be.Data.EntityFrameworkCore.Sqlite;
using WoW.Two.Sdk.Beta.Be.Data.Migrations.Bespoke;
using WoW.Two.Sdk.Beta.Be.Files.Audit;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Foundation.Naming;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Persistence;

/// <summary>Points <see cref="FilesDbContext"/> at the store's engine: Npgsql with the module's native enums, or SQLite.</summary>
internal static class FilesStoreProviderExtensions
{
    /// <summary>Holds the PostgreSQL enum <see cref="FileKind"/> maps to.</summary>
    public const string KindType = "files_file_kind";

    /// <summary>Holds the PostgreSQL enum <see cref="FileVisibility"/> maps to.</summary>
    public const string VisibilityType = "files_file_visibility";

    /// <summary>Holds the PostgreSQL enum <see cref="FileState"/> maps to.</summary>
    public const string StateType = "files_file_state";

    /// <summary>Holds the PostgreSQL enum <see cref="FileAuditAction"/> maps to.</summary>
    public const string AuditActionType = "files_file_audit_action";

    /// <summary>Holds the PostgreSQL enum <see cref="FileAuditOutcome"/> maps to.</summary>
    public const string AuditOutcomeType = "files_file_audit_outcome";

    /// <summary>Gets the translator of enum members to their PostgreSQL labels: snake_case (<c>RawPhoto</c> →
    /// <c>raw_photo</c>), as the SQL in <c>Migrations/Postgres/</c> spells them.</summary>
    public static INpgsqlNameTranslator Labels { get; } = new CaseStyleNameTranslator(CaseStyle.Snake);

    /// <summary>Configures the context for the store's engine and connection string.</summary>
    /// <param name="builder">The context options to configure.</param>
    /// <param name="options">The module's settings.</param>
    /// <returns>The same builder.</returns>
    public static DbContextOptionsBuilder UseFilesStore(this DbContextOptionsBuilder builder, FilesModuleOptions options) =>
        options.Provider == DatabaseProvider.Sqlite
            ? builder.UseSqliteConventional(options.ConnectionString)
            : builder.UseNpgsqlConventional(options.ConnectionString, MapEnums);

    /// <summary>Maps the module's enums to their native PostgreSQL types, on the context and on the data source EF builds.</summary>
    private static void MapEnums(NpgsqlDbContextOptionsBuilder npgsql) => npgsql
        .MapEnum<FileKind>(KindType, nameTranslator: Labels)
        .MapEnum<FileVisibility>(VisibilityType, nameTranslator: Labels)
        .MapEnum<FileState>(StateType, nameTranslator: Labels)
        .MapEnum<FileAuditAction>(AuditActionType, nameTranslator: Labels)
        .MapEnum<FileAuditOutcome>(AuditOutcomeType, nameTranslator: Labels);
}
