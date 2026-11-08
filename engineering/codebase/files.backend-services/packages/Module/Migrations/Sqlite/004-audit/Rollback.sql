-- Inverts 004-audit — drops the audit trail with its rows. Dev/test recovery only: the history is lost.

DROP TABLE IF EXISTS files_audit;
