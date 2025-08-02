CREATE TABLE "FeatureTemplate" (
	"Name"	TEXT NOT NULL UNIQUE COLLATE NOCASE,
	"HelpPageId"	INTEGER,
	"Description"	TEXT,
	"TemplateText"	TEXT,
	PRIMARY KEY("Name"),
	FOREIGN KEY("HelpPageId") REFERENCES "Page"("Id")
);
