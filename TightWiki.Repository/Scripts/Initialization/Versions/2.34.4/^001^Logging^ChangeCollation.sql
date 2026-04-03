BEGIN TRANSACTION;

PRAGMA foreign_keys = OFF;

CREATE TABLE "Severity_new" (
    "Id"    INTEGER NOT NULL,
    "Name"  TEXT NOT NULL COLLATE NOCASE UNIQUE,
    PRIMARY KEY("Id" AUTOINCREMENT)
);

INSERT INTO Severity_new (Id, Name)
SELECT Id, Name FROM Severity;

DROP TABLE Severity;

ALTER TABLE Severity_new RENAME TO Severity;

PRAGMA foreign_keys = ON;

COMMIT;
