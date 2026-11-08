-- Inverts 006-owner-issuer — drops the issuer and actor columns with their values. Dev/test recovery only.

ALTER TABLE files_audit DROP COLUMN actor_id;
ALTER TABLE files_audit DROP COLUMN subject_issuer;
ALTER TABLE files_files DROP COLUMN owner_issuer;
