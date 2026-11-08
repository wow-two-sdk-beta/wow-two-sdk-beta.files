-- 005-greetings-removed — drops the domain template's greeting log, which v0.1 replaced with the Files tables.

DROP TABLE IF EXISTS files_greeting_logs;
