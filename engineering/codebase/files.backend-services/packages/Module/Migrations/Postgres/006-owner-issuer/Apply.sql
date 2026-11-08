-- 006-owner-issuer — keys people as (issuer, subject): on a shared host a `sub` alone is not unique (PostgreSQL).
--
-- `files_files.owner_issuer` sits beside `owner_id`; `files_audit.subject_issuer` beside `subject_id`, and
-- `files_audit.actor_id` records the app acting for the person under token exchange. All NULL for this instance's own
-- API keys, in-process calls and rows written before this migration. Issuers and apps set no length, so these are text.

ALTER TABLE files_files ADD COLUMN IF NOT EXISTS owner_issuer text NULL;
ALTER TABLE files_audit ADD COLUMN IF NOT EXISTS subject_issuer text NULL;
ALTER TABLE files_audit ADD COLUMN IF NOT EXISTS actor_id text NULL;
