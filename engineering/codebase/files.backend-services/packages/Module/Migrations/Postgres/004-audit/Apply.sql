-- 004-audit — the audit trail: who uploaded, read, changed, deleted or restored a tenant's files (PostgreSQL).
--
-- One row per call, refused uploads included (no file id): an append-only log keyed by an identity, `seq`, which also
-- orders a tenant's trail for paging. Action and outcome are native enums; `error_code` is the refusal's be sdk
-- AppErrorType name, a set the kernel owns, so it stays text. `bytes` is what an upload received or a download served.

DO $$
BEGIN
    CREATE TYPE files_file_audit_action AS ENUM ('upload', 'download', 'update', 'delete', 'restore');
EXCEPTION
    WHEN duplicate_object THEN NULL;
END $$;

DO $$
BEGIN
    CREATE TYPE files_file_audit_outcome AS ENUM ('ok', 'refused');
EXCEPTION
    WHEN duplicate_object THEN NULL;
END $$;

CREATE TABLE IF NOT EXISTS files_audit (
    seq         bigint                   GENERATED ALWAYS AS IDENTITY,
    tenant_id   varchar(63)              NOT NULL,
    file_id     uuid                     NULL,
    action      files_file_audit_action  NOT NULL,
    outcome     files_file_audit_outcome NOT NULL,
    error_code  varchar(64)              NULL,
    subject_id  text                     NULL,
    app_id      text                     NULL,
    bytes       bigint                   NULL,
    occurred_at timestamptz              NOT NULL,
    CONSTRAINT pk_files_audit PRIMARY KEY (seq)
);

CREATE INDEX IF NOT EXISTS ix_files_audit_tenant_seq ON files_audit (tenant_id, seq);
CREATE INDEX IF NOT EXISTS ix_files_audit_file ON files_audit (file_id) WHERE file_id IS NOT NULL;
