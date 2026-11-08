-- 008-variants — image variants made on upload: one row per (file, width) (PostgreSQL).
--
-- `files_variants` points at the source file by `file_id` and at its bytes by `(tenant_id, sha256)`, a `files_blobs`
-- row like any file's, so the bytes dedup within the tenant and the sweep spares them while a variant uses them. A
-- variant goes with its source: served while the source is, purged with it.

CREATE TABLE IF NOT EXISTS files_variants (
    tenant_id    varchar(63)  NOT NULL,
    file_id      uuid         NOT NULL,
    width        integer      NOT NULL,
    height       integer      NOT NULL,
    content_type varchar(255) NOT NULL,
    size         bigint       NOT NULL,
    sha256       varchar(64)  NOT NULL,
    created_at   timestamptz  NOT NULL,
    CONSTRAINT pk_files_variants PRIMARY KEY (file_id, width)
);

CREATE INDEX IF NOT EXISTS ix_files_variants_tenant_blob ON files_variants (tenant_id, sha256);
