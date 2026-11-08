-- Inverts 002-files — drops the file rows, the blob index and their enum types. Dev/test recovery only: the stored
-- bytes stay in the blob store, unreferenced.

DROP TABLE IF EXISTS files_files;
DROP TABLE IF EXISTS files_blobs;
DROP TYPE IF EXISTS files_file_state;
DROP TYPE IF EXISTS files_file_visibility;
DROP TYPE IF EXISTS files_file_kind;
