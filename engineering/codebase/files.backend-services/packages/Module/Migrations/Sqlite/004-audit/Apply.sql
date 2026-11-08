-- 004-audit — the audit trail: who uploaded, read, changed, deleted or restored a tenant's files.
--
-- One row per call, refused uploads included (no file id). `seq` orders a tenant's trail for paging; action, outcome
-- and error code are enum names; `bytes` is what an upload received or a download served.

CREATE TABLE files_audit (
    seq              INTEGER PRIMARY KEY AUTOINCREMENT,
    tenant_id        TEXT    NOT NULL,
    file_id          TEXT    NULL,
    action           TEXT    NOT NULL,
    outcome          TEXT    NOT NULL,
    error_code       TEXT    NULL,
    subject_id       TEXT    NULL,
    app_id           TEXT    NULL,
    bytes            INTEGER NULL,
    occurred_at_utc  INTEGER NOT NULL
);

CREATE INDEX ix_files_audit_tenant_seq ON files_audit (tenant_id, seq);
CREATE INDEX ix_files_audit_file ON files_audit (file_id) WHERE file_id IS NOT NULL;
