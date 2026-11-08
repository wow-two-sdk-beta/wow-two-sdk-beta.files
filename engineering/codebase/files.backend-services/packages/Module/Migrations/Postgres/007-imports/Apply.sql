-- 007-imports — a product's legacy files moving into a tenant: an import, its sources, and each moved key (PostgreSQL).
--
-- `files_imports` holds one row per import, keyed by a uuid v7; `state` is `open` in P1, a code-side constant rather
-- than a CLR enum, so it stays text. `files_import_sources` lists an import's sources with the pushes each refused.
-- `files_import_items` is the id map the product's cutover reads: one row per (import, source, key), the file it became
-- and the reference attached, appended once and keyed by an identity, `seq`, which orders it for paging. A source id is
-- 100 characters at most, a key 1024 (FileImportConstants), a reference {recordType}:{recordId} 401.

CREATE TABLE IF NOT EXISTS files_imports (
    id         uuid        NOT NULL,
    tenant_id  varchar(63) NOT NULL,
    state      varchar(32) NOT NULL,
    created_at timestamptz NOT NULL,
    CONSTRAINT pk_files_imports PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS files_import_sources (
    import_id uuid         NOT NULL,
    source_id varchar(100) NOT NULL,
    failed    bigint       NOT NULL,
    CONSTRAINT pk_files_import_sources PRIMARY KEY (import_id, source_id)
);

CREATE TABLE IF NOT EXISTS files_import_items (
    seq        bigint        GENERATED ALWAYS AS IDENTITY,
    import_id  uuid          NOT NULL,
    source_id  varchar(100)  NOT NULL,
    source_key varchar(1024) NOT NULL,
    file_id    uuid          NOT NULL,
    sha256     varchar(64)   NOT NULL,
    size       bigint        NOT NULL,
    reference  varchar(401)  NULL,
    created_at timestamptz   NOT NULL,
    CONSTRAINT pk_files_import_items PRIMARY KEY (seq),
    CONSTRAINT uq_files_import_items_key UNIQUE (import_id, source_id, source_key)
);

CREATE INDEX IF NOT EXISTS ix_files_import_items_import_seq ON files_import_items (import_id, seq);
