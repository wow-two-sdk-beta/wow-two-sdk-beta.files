-- Inverts 002-files — drops the file rows and the blob index. Dev/test recovery only: the stored bytes stay in the
-- blob store, unreferenced.

DROP TABLE IF EXISTS files_files;
DROP TABLE IF EXISTS files_blobs;
