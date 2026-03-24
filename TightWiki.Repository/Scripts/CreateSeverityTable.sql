CREATE TABLE "Severity" (
	"Id"	INTEGER NOT NULL,
	"Name"	text NOT NULL UNIQUE,
	PRIMARY KEY("Id" AUTOINCREMENT)
);
INSERT INTO Severity(Name) VALUES ('Trace'), ('Debug'), ('Information'), ('Warning'), ('Error'), ('Critical'), ('None');
