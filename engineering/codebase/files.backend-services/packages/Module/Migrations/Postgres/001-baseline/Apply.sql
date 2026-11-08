-- 001-baseline — the PostgreSQL set's empty baseline.
--
-- The canonical schema: FilesDbContext maps over this set and never generates it. Tables carry the `files_` prefix and
-- enum types the `files_` prefix too, since a Form 1 consumer applies the set to its own database beside its own
-- objects. The SQLite set's 001 created the domain template's greeting log, which 005 drops; a template baseline ships
-- without a sample table, so this one creates nothing and keeps the ordinal.
--
-- Rules for the set: ids are uuid v7, made in code; instants are timestamptz named *_at; enums are native types with
-- snake_case labels; lengths are the contract's. Every statement is guarded, so a rerun succeeds and changes nothing.

SELECT 1;
