DROP TABLE IF EXISTS DefaultThemes;

CREATE TABLE DefaultThemes (
	Name	TEXT NOT NULL UNIQUE,
	DelimitedFiles	TEXT NOT NULL,
	ClassNavBar	TEXT NOT NULL,
	ClassNavLink	TEXT NOT NULL,
	ClassDropdown	TEXT NOT NULL,
	ClassBranding	TEXT NOT NULL,
	EditorTheme	TEXT NOT NULL,
	PRIMARY KEY(Name)
);
