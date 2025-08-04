CREATE TABLE "FeatureTemplate" (
	"Name"	TEXT NOT NULL UNIQUE COLLATE NOCASE,
	"Type"	TEXT NOT NULL COLLATE NOCASE,
	"PageId"	INTEGER,
	"Description"	TEXT,
	"TemplateText"	TEXT,
	PRIMARY KEY("Name"),
	FOREIGN KEY("PageId") REFERENCES "Page"("Id")
);
