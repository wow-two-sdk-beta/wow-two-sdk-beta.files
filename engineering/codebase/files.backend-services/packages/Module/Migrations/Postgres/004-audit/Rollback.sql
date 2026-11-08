-- Inverts 004-audit — drops the audit trail with its rows and its enum types. Dev/test recovery only: the history is
-- lost.

DROP TABLE IF EXISTS files_audit;
DROP TYPE IF EXISTS files_file_audit_outcome;
DROP TYPE IF EXISTS files_file_audit_action;
