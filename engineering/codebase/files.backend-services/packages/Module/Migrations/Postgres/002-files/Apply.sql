-- 002-files — stored files and their content-addressed bytes (PostgreSQL).
--
-- `files_blobs` holds each tenant's bytes once, keyed by SHA-256; `files_files` holds one row per upload, keyed by its
-- public FileId (a uuid v7), and points at a blob. `seq`, an identity, orders files by insertion for stable paging.
-- Kind, visibility and state are native enums whose labels are the CLR members in snake_case.

DO $$
BEGIN
    CREATE TYPE files_file_kind AS ENUM (
        'generic', 'image', 'raw_photo', 'vector', 'video', 'audio', 'pdf', 'document', 'spreadsheet', 'presentation',
        'text', 'markdown', 'ebook', 'code', 'data', 'database', 'config', 'archive', 'disk_image', 'executable', 'font',
        'model_3_d', 'cad', 'design', 'calendar', 'certificate', 'email');
EXCEPTION
    WHEN duplicate_object THEN NULL;
END $$;

DO $$
BEGIN
    CREATE TYPE files_file_visibility AS ENUM ('private', 'organization', 'public');
EXCEPTION
    WHEN duplicate_object THEN NULL;
END $$;

DO $$
BEGIN
    CREATE TYPE files_file_state AS ENUM ('uploading', 'pending', 'ready', 'rejected', 'quarantined');
EXCEPTION
    WHEN duplicate_object THEN NULL;
END $$;

-- storage_key is {tenant_id}/{sha256[..2]}/{sha256}: 63 + 1 + 2 + 1 + 64 characters at most.
CREATE TABLE IF NOT EXISTS files_blobs (
    tenant_id    varchar(63)  NOT NULL,
    sha256       varchar(64)  NOT NULL,
    storage_key  varchar(131) NOT NULL,
    size         bigint       NOT NULL,
    created_at   timestamptz  NOT NULL,
    last_used_at timestamptz  NOT NULL,
    CONSTRAINT pk_files_blobs PRIMARY KEY (tenant_id, sha256)
);

-- Organization, app, owner and idempotency key come from callers' tokens, keys and requests, which set no length.
CREATE TABLE IF NOT EXISTS files_files (
    seq             bigint                GENERATED ALWAYS AS IDENTITY,
    id              uuid                  NOT NULL,
    tenant_id       varchar(63)           NOT NULL,
    organization_id text                  NULL,
    app_id          text                  NULL,
    owner_id        text                  NULL,
    name            varchar(255)          NOT NULL,
    content_type    varchar(255)          NOT NULL,
    kind            files_file_kind       NOT NULL,
    size            bigint                NOT NULL,
    sha256          varchar(64)           NOT NULL,
    visibility      files_file_visibility NOT NULL,
    state           files_file_state      NOT NULL,
    idempotency_key text                  NULL,
    created_at      timestamptz           NOT NULL,
    expires_at      timestamptz           NULL,
    deleted_at      timestamptz           NULL,
    purge_at        timestamptz           NULL,
    CONSTRAINT pk_files_files PRIMARY KEY (id),
    CONSTRAINT uq_files_files_seq UNIQUE (seq)
);

CREATE INDEX IF NOT EXISTS ix_files_files_tenant_seq ON files_files (tenant_id, seq);
CREATE INDEX IF NOT EXISTS ix_files_files_tenant_blob ON files_files (tenant_id, sha256);
-- A retry with the key returns the first file, deleted or not, as on SQLite.
CREATE UNIQUE INDEX IF NOT EXISTS uq_files_files_tenant_idempotency ON files_files (tenant_id, idempotency_key)
    WHERE idempotency_key IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_files_files_purge ON files_files (purge_at) WHERE purge_at IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_files_files_expiry ON files_files (expires_at) WHERE expires_at IS NOT NULL;
