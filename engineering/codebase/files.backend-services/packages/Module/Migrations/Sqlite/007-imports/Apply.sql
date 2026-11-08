-- 007-imports — a product's legacy files moving into a tenant: an import, its sources, and each moved key.
--
-- `files_imports` holds one row per import, `id` a lowercase UUIDv7; `state` is `open` in P1. `files_import_sources`
-- lists an import's sources with the pushes each refused. `files_import_items` is the id map the product's cutover
-- reads: one row per (import, source, key), the file it became and the reference attached; `seq` orders it for paging.
-- Times are DateTimeOffsets stored as sortable INTEGERs (EF's binary conversion).

CREATE TABLE files_imports (
    id              TEXT    NOT NULL,
    tenant_id       TEXT    NOT NULL,
    state           TEXT    NOT NULL,
    created_at_utc  INTEGER NOT NULL,

    CONSTRAINT pk_files_imports PRIMARY KEY (id)
);

CREATE TABLE files_import_sources (
    import_id  TEXT    NOT NULL,
    source_id  TEXT    NOT NULL,
    failed     INTEGER NOT NULL DEFAULT 0,

    CONSTRAINT pk_files_import_sources PRIMARY KEY (import_id, source_id)
);

CREATE TABLE files_import_items (
    seq             INTEGER PRIMARY KEY AUTOINCREMENT,
    import_id       TEXT    NOT NULL,
    source_id       TEXT    NOT NULL,
    source_key      TEXT    NOT NULL,
    file_id         TEXT    NOT NULL,
    sha256          TEXT    NOT NULL,
    size            INTEGER NOT NULL,
    reference       TEXT    NULL,
    created_at_utc  INTEGER NOT NULL,

    CONSTRAINT uq_files_import_items_key UNIQUE (import_id, source_id, source_key)
);

CREATE INDEX ix_files_import_items_import_seq ON files_import_items (import_id, seq);
