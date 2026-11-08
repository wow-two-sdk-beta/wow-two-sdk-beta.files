-- 003-references — which records use a file (PostgreSQL).
--
-- A file lives while any record references it. `orphan_purge_at` is when an unreferenced file of an app with an orphan
-- retention gets purged; attaching clears it, the last detach sets it again. Record type and id are 200 characters at
-- most (FilesConstants.MaxReferenceLength).

CREATE TABLE IF NOT EXISTS files_references (
    tenant_id   varchar(63)  NOT NULL,
    file_id     uuid         NOT NULL,
    record_type varchar(200) NOT NULL,
    record_id   varchar(200) NOT NULL,
    created_at  timestamptz  NOT NULL,
    CONSTRAINT pk_files_references PRIMARY KEY (file_id, record_type, record_id)
);

CREATE INDEX IF NOT EXISTS ix_files_references_record ON files_references (tenant_id, record_type, record_id);

ALTER TABLE files_files ADD COLUMN IF NOT EXISTS orphan_purge_at timestamptz NULL;

CREATE INDEX IF NOT EXISTS ix_files_files_orphan ON files_files (orphan_purge_at) WHERE orphan_purge_at IS NOT NULL;
