-- 003-references — which records use a file.
--
-- A file lives while any record references it. `orphan_purge_at_utc` is when an unreferenced file of an app with an
-- orphan retention gets purged; attaching clears it, the last detach sets it again.

CREATE TABLE files_references (
    tenant_id       TEXT    NOT NULL,
    file_id         TEXT    NOT NULL,
    record_type     TEXT    NOT NULL,
    record_id       TEXT    NOT NULL,
    created_at_utc  INTEGER NOT NULL,

    CONSTRAINT pk_files_references PRIMARY KEY (file_id, record_type, record_id)
);

CREATE INDEX ix_files_references_record ON files_references (tenant_id, record_type, record_id);

ALTER TABLE files_files ADD COLUMN orphan_purge_at_utc INTEGER NULL;

CREATE INDEX ix_files_files_orphan ON files_files (orphan_purge_at_utc) WHERE orphan_purge_at_utc IS NOT NULL;
