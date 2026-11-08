-- Inverts 007-imports — drops every import, its sources and its id map. Dev/test recovery only: the moved files stay,
-- but no map points at them.

DROP INDEX IF EXISTS ix_files_import_items_import_seq;
DROP TABLE IF EXISTS files_import_items;
DROP TABLE IF EXISTS files_import_sources;
DROP TABLE IF EXISTS files_imports;
