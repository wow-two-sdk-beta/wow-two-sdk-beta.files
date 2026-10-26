-- 001-baseline — the greeting log.
--
-- The canonical schema: EF maps over this and never generates it. Tables carry the `files_` prefix, since a Form 1
-- consumer applies them to its own database beside its own tables. `id` is a Guid stored as TEXT; `greeted_at_utc` is
-- a DateTimeOffset stored as a sortable INTEGER (EF's binary conversion).

CREATE TABLE files_greeting_logs (
    id             TEXT    NOT NULL,
    message        TEXT    NOT NULL,
    greeted_at_utc INTEGER NOT NULL,

    CONSTRAINT pk_files_greeting_logs PRIMARY KEY (id)
);
