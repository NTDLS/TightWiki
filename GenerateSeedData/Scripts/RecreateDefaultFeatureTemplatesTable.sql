DROP TABLE IF EXISTS DefaultFeatureTemplates;

CREATE TABLE DefaultFeatureTemplates (
	Name TEXT NOT NULL COLLATE NOCASE,
	Type TEXT COLLATE NOCASE,
	PageName TEXT COLLATE NOCASE,
	Description TEXT,
	TemplateText TEXT,
	PRIMARY KEY(Name,Type)
);
