CREATE TABLE "Severity" (
	"Id"	INTEGER NOT NULL,
	"Name"	TEXT NOT NULL UNIQUE COLLATE NOCASE,
	PRIMARY KEY("Id" AUTOINCREMENT)
);
INSERT INTO Severity(Name) VALUES ('Trace'), ('Debug'), ('Information'), ('Warning'), ('Error'), ('Critical'), ('None');
