-- Inverts 003-references — drops the reference rows and the orphan times. Dev/test recovery only: every file becomes
-- unreferenced.

DROP INDEX IF EXISTS ix_files_files_orphan;
ALTER TABLE files_files DROP COLUMN orphan_purge_at_utc;
DROP TABLE IF EXISTS files_references;
