using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using WoW.Two.Sdk.Beta.Be.Files.Module.Imports;

namespace WoW.Two.Sdk.Beta.Be.Files.Module.Persistence;

/// <summary>The Files module's context — a pure mapper over the SQL-owned schema in <c>Migrations/</c>.</summary>
/// <remarks>Internal: consumers reach the store through the Files brokers only, never its tables.</remarks>
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

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite cannot ORDER BY a TEXT-stored DateTimeOffset — store it as a sortable binary long instead.
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var file = modelBuilder.Entity<FileEntity>();
        file.ToTable(FileEntity.TableName);
        file.HasKey(row => row.Seq);
        file.Property(row => row.Seq).HasColumnName("seq").ValueGeneratedOnAdd();
        file.Property(row => row.Id).HasColumnName("id");
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
        file.Property(row => row.CreatedAtUtc).HasColumnName("created_at_utc");
        file.Property(row => row.ExpiresAtUtc).HasColumnName("expires_at_utc");
        file.Property(row => row.DeletedAtUtc).HasColumnName("deleted_at_utc");
        file.Property(row => row.PurgeAtUtc).HasColumnName("purge_at_utc");
        file.Property(row => row.OrphanPurgeAtUtc).HasColumnName("orphan_purge_at_utc");

        var blob = modelBuilder.Entity<FileBlobEntity>();
        blob.ToTable(FileBlobEntity.TableName);
        blob.HasKey(row => new { row.TenantId, row.Sha256 });
        blob.Property(row => row.TenantId).HasColumnName("tenant_id");
        blob.Property(row => row.Sha256).HasColumnName("sha256");
        blob.Property(row => row.StorageKey).HasColumnName("storage_key");
        blob.Property(row => row.Size).HasColumnName("size");
        blob.Property(row => row.CreatedAtUtc).HasColumnName("created_at_utc");
        blob.Property(row => row.LastUsedAtUtc).HasColumnName("last_used_at_utc");

        var reference = modelBuilder.Entity<FileReferenceEntity>();
        reference.ToTable(FileReferenceEntity.TableName);
        reference.HasKey(row => new { row.FileId, row.RecordType, row.RecordId });
        reference.Property(row => row.TenantId).HasColumnName("tenant_id");
        reference.Property(row => row.FileId).HasColumnName("file_id");
        reference.Property(row => row.RecordType).HasColumnName("record_type");
        reference.Property(row => row.RecordId).HasColumnName("record_id");
        reference.Property(row => row.CreatedAtUtc).HasColumnName("created_at_utc");

        var variant = modelBuilder.Entity<FileVariantEntity>();
        variant.ToTable(FileVariantEntity.TableName);
        variant.HasKey(row => new { row.FileId, row.Width });
        variant.Property(row => row.TenantId).HasColumnName("tenant_id");
        variant.Property(row => row.FileId).HasColumnName("file_id");
        variant.Property(row => row.Width).HasColumnName("width").ValueGeneratedNever();
        variant.Property(row => row.Height).HasColumnName("height");
        variant.Property(row => row.ContentType).HasColumnName("content_type");
        variant.Property(row => row.Size).HasColumnName("size");
        variant.Property(row => row.Sha256).HasColumnName("sha256");
        variant.Property(row => row.CreatedAtUtc).HasColumnName("created_at_utc");

        var audit = modelBuilder.Entity<FileAuditEntity>();
        audit.ToTable(FileAuditEntity.TableName);
        audit.HasKey(row => row.Seq);
        audit.Property(row => row.Seq).HasColumnName("seq").ValueGeneratedOnAdd();
        audit.Property(row => row.TenantId).HasColumnName("tenant_id");
        audit.Property(row => row.FileId).HasColumnName("file_id");
        audit.Property(row => row.Action).HasColumnName("action");
        audit.Property(row => row.Outcome).HasColumnName("outcome");
        audit.Property(row => row.ErrorCode).HasColumnName("error_code");
        audit.Property(row => row.SubjectId).HasColumnName("subject_id");
        audit.Property(row => row.SubjectIssuer).HasColumnName("subject_issuer");
        audit.Property(row => row.ActorId).HasColumnName("actor_id");
        audit.Property(row => row.AppId).HasColumnName("app_id");
        audit.Property(row => row.Bytes).HasColumnName("bytes");
        audit.Property(row => row.OccurredAtUtc).HasColumnName("occurred_at_utc");

        var import = modelBuilder.Entity<FileImportEntity>();
        import.ToTable(FileImportEntity.TableName);
        import.HasKey(row => row.Id);
        import.Property(row => row.Id).HasColumnName("id");
        import.Property(row => row.TenantId).HasColumnName("tenant_id");
        import.Property(row => row.State).HasColumnName("state");
        import.Property(row => row.CreatedAtUtc).HasColumnName("created_at_utc");

        var importSource = modelBuilder.Entity<FileImportSourceEntity>();
        importSource.ToTable(FileImportSourceEntity.TableName);
        importSource.HasKey(row => new { row.ImportId, row.SourceId });
        importSource.Property(row => row.ImportId).HasColumnName("import_id");
        importSource.Property(row => row.SourceId).HasColumnName("source_id");
        importSource.Property(row => row.Failed).HasColumnName("failed");

        var importItem = modelBuilder.Entity<FileImportItemEntity>();
        importItem.ToTable(FileImportItemEntity.TableName);
        importItem.HasKey(row => row.Seq);
        importItem.Property(row => row.Seq).HasColumnName("seq").ValueGeneratedOnAdd();
        importItem.Property(row => row.ImportId).HasColumnName("import_id");
        importItem.Property(row => row.SourceId).HasColumnName("source_id");
        importItem.Property(row => row.SourceKey).HasColumnName("source_key");
        importItem.Property(row => row.FileId).HasColumnName("file_id");
        importItem.Property(row => row.Sha256).HasColumnName("sha256");
        importItem.Property(row => row.Size).HasColumnName("size");
        importItem.Property(row => row.Reference).HasColumnName("reference");
        importItem.Property(row => row.CreatedAtUtc).HasColumnName("created_at_utc");
    }
}
