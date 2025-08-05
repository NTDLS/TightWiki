CREATE TABLE "FeatureTemplate" (
	"Name"	TEXT NOT NULL COLLATE NOCASE,
	"Type"	TEXT COLLATE NOCASE,
	"PageId"	INTEGER,
	"Description"	TEXT,
	"TemplateText"	TEXT,
	PRIMARY KEY("Name","Type"),
	FOREIGN KEY("PageId") REFERENCES "Page"("Id")
);
