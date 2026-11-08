-- Inverts 006-owner-issuer — drops the issuer and actor columns with their values. Dev/test recovery only.

ALTER TABLE IF EXISTS files_audit DROP COLUMN IF EXISTS actor_id;
ALTER TABLE IF EXISTS files_audit DROP COLUMN IF EXISTS subject_issuer;
ALTER TABLE IF EXISTS files_files DROP COLUMN IF EXISTS owner_issuer;
