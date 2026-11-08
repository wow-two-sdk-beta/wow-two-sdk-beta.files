using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using WoW.Two.Sdk.Beta.Be.Files.Audit;
using WoW.Two.Sdk.Beta.Be.Files.Module.Imports;
using WoW.Two.Sdk.Beta.Be.Files.Store;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Persistence;

/// <summary>The Files module's context — a pure mapper over the SQL-owned schema in <c>Migrations/{Postgres,Sqlite}/</c>.</summary>
/// <remarks>
///   - Internal: consumers reach the store through the Files brokers only, never its tables.
///   - One model over two shapes. PostgreSQL: <c>uuid</c> ids, <c>timestamptz</c> instants named <c>*_at</c>, native
///     enums. SQLite: TEXT ids, instants as sortable INTEGERs named <c>*_at_utc</c>, enum names as TEXT.
/// </remarks>
/// <param name="options">The context options, configured by <c>AddFiles</c>.</param>
internal sealed class FilesDbContext(DbContextOptions<FilesDbContext> options) : DbContext(options)
{
    /// <summary>Gets the stored files.</summary>
    public DbSet<FileEntity> Files => Set<FileEntity>();

    /// <summary>Gets the stored bytes, one row per tenant and SHA-256.</summary>
    public DbSet<FileBlobEntity> Blobs => Set<FileBlobEntity>();

    /// <summary>Gets the records' uses of files.</summary>
    public DbSet<FileReferenceEntity> References => Set<FileReferenceEntity>();

    /// <summary>Gets the files' image variants, one row per file and width.</summary>
    public DbSet<FileVariantEntity> Variants => Set<FileVariantEntity>();

    /// <summary>Gets the audit trail.</summary>
    public DbSet<FileAuditEntity> Audit => Set<FileAuditEntity>();

    /// <summary>Gets the imports: legacy files moving into a tenant.</summary>
    public DbSet<FileImportEntity> Imports => Set<FileImportEntity>();

    /// <summary>Gets each import's sources.</summary>
    public DbSet<FileImportSourceEntity> ImportSources => Set<FileImportSourceEntity>();

    /// <summary>Gets each import's moved keys: the id map.</summary>
    public DbSet<FileImportItemEntity> ImportItems => Set<FileImportItemEntity>();

    /// <summary>Gets a tenant's live files: not past their purge or orphan time, and not expired unless a record still
    /// references them.</summary>
    /// <param name="tenantId">The tenant.</param>
    /// <param name="now">The current time.</param>
    /// <returns>The query, tracked.</returns>
    public IQueryable<FileEntity> LiveFiles(string tenantId, DateTimeOffset now) =>
        Files.Where(file =>
            file.TenantId == tenantId
            && (file.PurgeAtUtc == null || file.PurgeAtUtc > now)
            && (file.OrphanPurgeAtUtc == null || file.OrphanPurgeAtUtc > now)
            && (file.ExpiresAtUtc == null || file.ExpiresAtUtc > now || References.Any(reference => reference.FileId == file.Id)));

    /// <summary>Gets whether the store runs on PostgreSQL rather than SQLite.</summary>
    private bool OnPostgres => !Database.IsSqlite();

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        if (OnPostgres)
        {
            // timestamptz takes an instant at offset zero only; a caller's expiry may carry another offset.
            configurationBuilder.Properties<DateTimeOffset>().HaveConversion<UtcInstantConverter>();
            return;
        }

        // SQLite cannot ORDER BY a TEXT-stored DateTimeOffset — store it as a sortable binary long instead — and has no
        // enum type, so the enums keep their names as TEXT.
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
        configurationBuilder.Properties<FileKind>().HaveConversion<string>();
        configurationBuilder.Properties<FileVisibility>().HaveConversion<string>();
        configurationBuilder.Properties<FileState>().HaveConversion<string>();
        configurationBuilder.Properties<FileAuditAction>().HaveConversion<string>();
        configurationBuilder.Properties<FileAuditOutcome>().HaveConversion<string>();
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var file = modelBuilder.Entity<FileEntity>();
        file.ToTable(FileEntity.TableName);
        file.HasKey(row => row.Seq);
        file.Property(row => row.Seq).HasColumnName("seq").ValueGeneratedOnAdd();
        Uuid(file.Property(row => row.Id).HasColumnName("id"));
        file.Property(row => row.TenantId).HasColumnName("tenant_id");
        file.Property(row => row.OrganizationId).HasColumnName("organization_id");
        file.Property(row => row.AppId).HasColumnName("app_id");
        file.Property(row => row.OwnerId).HasColumnName("owner_id");
        file.Property(row => row.OwnerIssuer).HasColumnName("owner_issuer");
        file.Property(row => row.Name).HasColumnName("name");
        file.Property(row => row.ContentType).HasColumnName("content_type");
        file.Property(row => row.Kind).HasColumnName("kind");
        file.Property(row => row.Size).HasColumnName("size");
        file.Property(row => row.Sha256).HasColumnName("sha256");
        file.Property(row => row.Visibility).HasColumnName("visibility");
        file.Property(row => row.State).HasColumnName("state");
        file.Property(row => row.IdempotencyKey).HasColumnName("idempotency_key");
        file.Property(row => row.CreatedAtUtc).HasColumnName(At("created_at"));
        file.Property(row => row.ExpiresAtUtc).HasColumnName(At("expires_at"));
        file.Property(row => row.DeletedAtUtc).HasColumnName(At("deleted_at"));
        file.Property(row => row.PurgeAtUtc).HasColumnName(At("purge_at"));
        file.Property(row => row.OrphanPurgeAtUtc).HasColumnName(At("orphan_purge_at"));

        var blob = modelBuilder.Entity<FileBlobEntity>();
        blob.ToTable(FileBlobEntity.TableName);
        blob.HasKey(row => new { row.TenantId, row.Sha256 });
        blob.Property(row => row.TenantId).HasColumnName("tenant_id");
        blob.Property(row => row.Sha256).HasColumnName("sha256");
        blob.Property(row => row.StorageKey).HasColumnName("storage_key");
        blob.Property(row => row.Size).HasColumnName("size");
        blob.Property(row => row.CreatedAtUtc).HasColumnName(At("created_at"));
        blob.Property(row => row.LastUsedAtUtc).HasColumnName(At("last_used_at"));

        var reference = modelBuilder.Entity<FileReferenceEntity>();
        reference.ToTable(FileReferenceEntity.TableName);
        reference.HasKey(row => new { row.FileId, row.RecordType, row.RecordId });
        reference.Property(row => row.TenantId).HasColumnName("tenant_id");
        Uuid(reference.Property(row => row.FileId).HasColumnName("file_id"));
        reference.Property(row => row.RecordType).HasColumnName("record_type");
        reference.Property(row => row.RecordId).HasColumnName("record_id");
        reference.Property(row => row.CreatedAtUtc).HasColumnName(At("created_at"));

        var variant = modelBuilder.Entity<FileVariantEntity>();
        variant.ToTable(FileVariantEntity.TableName);
        variant.HasKey(row => new { row.FileId, row.Width });
        variant.Property(row => row.TenantId).HasColumnName("tenant_id");
        Uuid(variant.Property(row => row.FileId).HasColumnName("file_id"));
        variant.Property(row => row.Width).HasColumnName("width").ValueGeneratedNever();
        variant.Property(row => row.Height).HasColumnName("height");
        variant.Property(row => row.ContentType).HasColumnName("content_type");
        variant.Property(row => row.Size).HasColumnName("size");
        variant.Property(row => row.Sha256).HasColumnName("sha256");
        variant.Property(row => row.CreatedAtUtc).HasColumnName(At("created_at"));

        var audit = modelBuilder.Entity<FileAuditEntity>();
        audit.ToTable(FileAuditEntity.TableName);
        audit.HasKey(row => row.Seq);
        audit.Property(row => row.Seq).HasColumnName("seq").ValueGeneratedOnAdd();
        audit.Property(row => row.TenantId).HasColumnName("tenant_id");
        Uuid(audit.Property(row => row.FileId).HasColumnName("file_id"));
        audit.Property(row => row.Action).HasColumnName("action");
        audit.Property(row => row.Outcome).HasColumnName("outcome");
        audit.Property(row => row.ErrorCode).HasColumnName("error_code");
        audit.Property(row => row.SubjectId).HasColumnName("subject_id");
        audit.Property(row => row.SubjectIssuer).HasColumnName("subject_issuer");
        audit.Property(row => row.ActorId).HasColumnName("actor_id");
        audit.Property(row => row.AppId).HasColumnName("app_id");
        audit.Property(row => row.Bytes).HasColumnName("bytes");
        audit.Property(row => row.OccurredAtUtc).HasColumnName(At("occurred_at"));

        var import = modelBuilder.Entity<FileImportEntity>();
        import.ToTable(FileImportEntity.TableName);
        import.HasKey(row => row.Id);
        Uuid(import.Property(row => row.Id).HasColumnName("id"));
        import.Property(row => row.TenantId).HasColumnName("tenant_id");
        import.Property(row => row.State).HasColumnName("state");
        import.Property(row => row.CreatedAtUtc).HasColumnName(At("created_at"));

        var importSource = modelBuilder.Entity<FileImportSourceEntity>();
        importSource.ToTable(FileImportSourceEntity.TableName);
        importSource.HasKey(row => new { row.ImportId, row.SourceId });
        Uuid(importSource.Property(row => row.ImportId).HasColumnName("import_id"));
        importSource.Property(row => row.SourceId).HasColumnName("source_id");
        importSource.Property(row => row.Failed).HasColumnName("failed");

        var importItem = modelBuilder.Entity<FileImportItemEntity>();
        importItem.ToTable(FileImportItemEntity.TableName);
        importItem.HasKey(row => row.Seq);
        importItem.Property(row => row.Seq).HasColumnName("seq").ValueGeneratedOnAdd();
        Uuid(importItem.Property(row => row.ImportId).HasColumnName("import_id"));
        importItem.Property(row => row.SourceId).HasColumnName("source_id");
        importItem.Property(row => row.SourceKey).HasColumnName("source_key");
        Uuid(importItem.Property(row => row.FileId).HasColumnName("file_id"));
        importItem.Property(row => row.Sha256).HasColumnName("sha256");
        importItem.Property(row => row.Size).HasColumnName("size");
        importItem.Property(row => row.Reference).HasColumnName("reference");
        importItem.Property(row => row.CreatedAtUtc).HasColumnName(At("created_at"));
    }

    /// <summary>Names an instant's column: <c>{stem}</c> on PostgreSQL, <c>{stem}_utc</c> on SQLite.</summary>
    /// <param name="stem">The column's name ending in <c>_at</c>.</param>
    private string At(string stem) => OnPostgres ? stem : stem + "_utc";

    /// <summary>Stores an id string as a <c>uuid</c> on PostgreSQL; SQLite keeps the TEXT.</summary>
    /// <param name="property">The id's property, already named.</param>
    private void Uuid(PropertyBuilder property)
    {
        if (OnPostgres)
            property.HasConversion(UuidTextConverter.Instance);
    }

    /// <summary>Converts an instant to offset zero on the way in, the only offset <c>timestamptz</c> takes.</summary>
    private sealed class UtcInstantConverter() : ValueConverter<DateTimeOffset, DateTimeOffset>(value => value.ToUniversalTime(), value => value);

    /// <summary>Converts a lowercase id string to the <c>uuid</c> it spells and back; text that spells none, such as a
    /// caller's malformed import id, becomes the empty uuid, which no row holds.</summary>
    private sealed class UuidTextConverter() : ValueConverter<string, Guid>(text => ToUuid(text), uuid => uuid.ToString("D"))
    {
        /// <summary>Gets the shared instance.</summary>
        public static UuidTextConverter Instance { get; } = new();

        /// <summary>Parses an id, or returns the empty uuid.</summary>
        internal static Guid ToUuid(string text) => Guid.TryParse(text, out var uuid) ? uuid : Guid.Empty;
    }
}
