--##IF TABLE NOT EXISTS(Permission)

CREATE TABLE "Permission" (
	"Id"	INTEGER NOT NULL UNIQUE,
	"Name"	TEXT NOT NULL UNIQUE COLLATE NOCASE,
	"Description"	TEXT,
	PRIMARY KEY("Id" AUTOINCREMENT)
);

--Insert seed data.
INSERT INTO Permission(Name, Description)
SELECT 'Read','User or role can read page or within namespace.'
UNION SELECT 'Edit','User or role can edit page or within namespace.'
UNION SELECT 'Delete','User or role can delete page or within namespace.'
UNION SELECT 'Moderate','User or role can moderate page or within namespace, such as editing protected pages and reverting changes.'
UNION SELECT 'Create','User or role can create pages.';
