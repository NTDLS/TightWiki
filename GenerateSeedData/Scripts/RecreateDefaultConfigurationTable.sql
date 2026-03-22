DROP TABLE IF EXISTS DefaultConfiguration;

CREATE TABLE DefaultConfiguration (
	ConfigurationGroupName TEXT NOT NULL COLLATE NOCASE,
	ConfigurationEntryName TEXT NOT NULL COLLATE NOCASE,
	Value	TEXT,
	DataTypeId	int NOT NULL,
	Description	TEXT COLLATE NOCASE,
	IsEncrypted	INTEGER NOT NULL,
	IsRequired	INTEGER NOT NULL
);