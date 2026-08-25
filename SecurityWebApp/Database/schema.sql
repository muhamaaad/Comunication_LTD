-- Comunication_LTD - database schema (SQLite)
--
-- Run this once, from the SecurityWebApp folder, before starting the app:
--
--     sqlite3 app.db ".read Database/schema.sql"
--
-- To start over: delete app.db and run it again.
--
-- The primary keys are plain INTEGER PRIMARY KEY, not AUTOINCREMENT. SQLite
-- still fills them in by itself, and this way it does not create its own
-- internal "sqlite_sequence" table.

PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS "Users" (
    "Id"                     INTEGER NOT NULL CONSTRAINT "PK_Users" PRIMARY KEY,
    "Username"               TEXT    NOT NULL COLLATE NOCASE,
    "Email"                  TEXT    NOT NULL COLLATE NOCASE,
    "PasswordHash"           TEXT    NOT NULL,
    "IsAdmin"                INTEGER NOT NULL DEFAULT 0,
    "CreatedAt"              TEXT    NOT NULL,
    "LastLogin"              TEXT    NULL,
    "LoginAttempts"          INTEGER NOT NULL,
    "LastFailedLoginAttempt" TEXT    NULL,
    "IsLocked"               INTEGER NOT NULL,
    "LockedUntil"            TEXT    NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Username" ON "Users" ("Username");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Email"    ON "Users" ("Email");

CREATE TABLE IF NOT EXISTS "PasswordHistories" (
    "Id"           INTEGER NOT NULL CONSTRAINT "PK_PasswordHistories" PRIMARY KEY,
    "UserId"       INTEGER NOT NULL,
    "PasswordHash" TEXT    NOT NULL,
    "ChangedAt"    TEXT    NOT NULL,
    CONSTRAINT "FK_PasswordHistories_Users_UserId"
        FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_PasswordHistories_UserId" ON "PasswordHistories" ("UserId");

CREATE TABLE IF NOT EXISTS "PasswordResetTokens" (
    "Id"         INTEGER NOT NULL CONSTRAINT "PK_PasswordResetTokens" PRIMARY KEY,
    "UserId"     INTEGER NOT NULL,
    "ResetToken" TEXT    NOT NULL,
    "Expiration" TEXT    NOT NULL,
    "Used"       INTEGER NOT NULL,
    CONSTRAINT "FK_PasswordResetTokens_Users_UserId"
        FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_PasswordResetTokens_ResetToken" ON "PasswordResetTokens" ("ResetToken");
CREATE INDEX        IF NOT EXISTS "IX_PasswordResetTokens_UserId"     ON "PasswordResetTokens" ("UserId");
