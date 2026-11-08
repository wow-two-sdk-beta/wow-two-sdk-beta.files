-- 002-files — stored files and their content-addressed bytes.
--
-- `files_blobs` holds each tenant's bytes once, keyed by SHA-256; `files_files` holds one row per upload and points at
-- a blob. `seq` orders files by insertion for stable paging; `id` is the public FileId (a lowercase UUIDv7). Times are
-- DateTimeOffsets stored as sortable INTEGERs (EF's binary conversion); kind, visibility and state are enum names.

CREATE TABLE files_blobs (
    tenant_id         TEXT    NOT NULL,
    sha256            TEXT    NOT NULL,
    storage_key       TEXT    NOT NULL,
    size              INTEGER NOT NULL,
    created_at_utc    INTEGER NOT NULL,
    last_used_at_utc  INTEGER NOT NULL,

    CONSTRAINT pk_files_blobs PRIMARY KEY (tenant_id, sha256)
);

CREATE TABLE files_files (
    seq               INTEGER PRIMARY KEY AUTOINCREMENT,
    id                TEXT    NOT NULL,
    tenant_id         TEXT    NOT NULL,
    organization_id   TEXT    NULL,
    app_id            TEXT    NULL,
    owner_id          TEXT    NULL,
    name              TEXT    NOT NULL,
    content_type      TEXT    NOT NULL,
    kind              TEXT    NOT NULL,
    size              INTEGER NOT NULL,
    sha256            TEXT    NOT NULL,
    visibility        TEXT    NOT NULL,
    state             TEXT    NOT NULL,
    idempotency_key   TEXT    NULL,
    created_at_utc    INTEGER NOT NULL,
    expires_at_utc    INTEGER NULL,
    deleted_at_utc    INTEGER NULL,
    purge_at_utc      INTEGER NULL,

    CONSTRAINT uq_files_files_id UNIQUE (id)
);

CREATE INDEX ix_files_files_tenant_seq ON files_files (tenant_id, seq);
CREATE INDEX ix_files_files_tenant_blob ON files_files (tenant_id, sha256);
CREATE UNIQUE INDEX uq_files_files_tenant_idempotency ON files_files (tenant_id, idempotency_key)
    WHERE idempotency_key IS NOT NULL;
CREATE INDEX ix_files_files_purge ON files_files (purge_at_utc) WHERE purge_at_utc IS NOT NULL;
CREATE INDEX ix_files_files_expiry ON files_files (expires_at_utc) WHERE expires_at_utc IS NOT NULL;
