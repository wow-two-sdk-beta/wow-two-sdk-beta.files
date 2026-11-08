-- 008-variants — image variants made on upload: one row per (file, width).
--
-- `files_variants` points at the source file by `file_id` and at its bytes by `(tenant_id, sha256)`, a `files_blobs`
-- row like any file's, so the bytes dedup within the tenant and the sweep spares them while a variant uses them. A
-- variant goes with its source: served while the source is, purged with it. Times are DateTimeOffsets stored as
-- sortable INTEGERs (EF's binary conversion).

CREATE TABLE files_variants (
    tenant_id       TEXT    NOT NULL,
    file_id         TEXT    NOT NULL,
    width           INTEGER NOT NULL,
    height          INTEGER NOT NULL,
    content_type    TEXT    NOT NULL,
    size            INTEGER NOT NULL,
    sha256          TEXT    NOT NULL,
    created_at_utc  INTEGER NOT NULL,

    CONSTRAINT pk_files_variants PRIMARY KEY (file_id, width)
);

CREATE INDEX ix_files_variants_tenant_blob ON files_variants (tenant_id, sha256);
