-- Inverts 005-greetings-removed — recreates the empty greeting log, so 001-baseline's rollback finds it.

CREATE TABLE files_greeting_logs (
    id             TEXT    NOT NULL,
    message        TEXT    NOT NULL,
    greeted_at_utc INTEGER NOT NULL,

    CONSTRAINT pk_files_greeting_logs PRIMARY KEY (id)
);
