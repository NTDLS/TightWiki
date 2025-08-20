--##IF TABLE NOT EXISTS(PermissionDisposition)

CREATE TABLE "PermissionDisposition" (
	"Id"	INTEGER NOT NULL UNIQUE,
	"Name"	TEXT NOT NULL UNIQUE COLLATE NOCASE,
	PRIMARY KEY("Id" AUTOINCREMENT)
);

--Insert seed data.
INSERT INTO PermissionDisposition(Name)
SELECT 'Allow'
UNION SELECT 'Deny'
