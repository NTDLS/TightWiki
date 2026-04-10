DROP TABLE IF EXISTS "Log";
DROP TABLE IF EXISTS "Severity";

CREATE TABLE "Severity" (
	"Id"	INTEGER NOT NULL,
	"Name"	TEXT NOT NULL UNIQUE COLLATE NOCASE,
	PRIMARY KEY("Id" AUTOINCREMENT)
);
INSERT INTO Severity(Name) VALUES ('Trace'), ('Debug'), ('Information'), ('Warning'), ('Error'), ('Critical'), ('None');


CREATE TABLE "Log" (
	"Id"	INTEGER,
	"SeverityId"	INTEGER,
	"Text"	text,
	"ExceptionText"	text,
	"StackTrace"	text,
	"CreatedDate"	text,
	PRIMARY KEY("Id" AUTOINCREMENT),
	FOREIGN KEY("SeverityId") REFERENCES "Severity"("Id")
);
