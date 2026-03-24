DROP TABLE IF EXISTS CompilationStatistics;

CREATE TABLE "CompilationStatistics" (
	"Id" INTEGER,
	"PageId" INTEGER NOT NULL,
	"LastCompileDateTime" TEXT NOT NULL,

	"TotalCompilationCount" INTEGER NOT NULL,
	"LastWikifyTimeMs" REAL,
	"TotalWikifyTimeMs" REAL,
	"LastMatchCount"	INTEGER,
	"LastErrorCount"	INTEGER,
	"LastOutgoingLinkCount"	INTEGER,
	"LastTagCount"	INTEGER,
	"LastProcessedBodySize"	INTEGER,
	"LastBodySize"	INTEGER,
	PRIMARY KEY("Id" AUTOINCREMENT)
);

CREATE UNIQUE INDEX IX_CompilationStatistics_PageId
ON CompilationStatistics(PageId);
