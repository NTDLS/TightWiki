CREATE TABLE "SecurityGroup" (
	"Id"	INTEGER NOT NULL UNIQUE,
	"Name"	TEXT NOT NULL UNIQUE COLLATE NOCASE,
	"Description"	TEXT,
	PRIMARY KEY("Id" AUTOINCREMENT)
);

INSERT INTO SecurityGroup(Name) SELECT 'Group One';
INSERT INTO SecurityGroup(Name) SELECT 'Group Two';
INSERT INTO SecurityGroup(Name) SELECT 'Group Three';
