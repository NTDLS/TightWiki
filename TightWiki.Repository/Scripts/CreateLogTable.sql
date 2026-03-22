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
