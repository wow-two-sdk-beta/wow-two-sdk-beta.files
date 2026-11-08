-- Inverts 003-references — drops the reference rows and the orphan times. Dev/test recovery only: every file becomes
-- unreferenced.

DROP INDEX IF EXISTS ix_files_files_orphan;
ALTER TABLE IF EXISTS files_files DROP COLUMN IF EXISTS orphan_purge_at;
DROP TABLE IF EXISTS files_references;
