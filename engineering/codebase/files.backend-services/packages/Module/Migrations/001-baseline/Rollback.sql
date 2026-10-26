-- Inverts 001-baseline — drops the greeting log with its rows. Dev/test recovery only; the log restarts empty.

DROP TABLE IF EXISTS files_greeting_logs;
