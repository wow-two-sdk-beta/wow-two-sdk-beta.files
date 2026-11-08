-- 005-greetings-removed — drops the domain template's greeting log, which v0.1 replaced with the Files tables.
--
-- The PostgreSQL set never created it (001-baseline is empty); the guarded drop keeps the ordinal and changes nothing.

DROP TABLE IF EXISTS files_greeting_logs;
