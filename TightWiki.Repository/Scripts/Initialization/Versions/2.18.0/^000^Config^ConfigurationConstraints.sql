BEGIN TRANSACTION;

ALTER TABLE ConfigurationEntry RENAME TO ConfigurationEntry_Upgrade;

CREATE TABLE "ConfigurationEntry" (
	"Id"	INTEGER NOT NULL,
	"ConfigurationGroupId"	INTEGER NOT NULL,
	"Name"	TEXT NOT NULL COLLATE NOCASE,
	"Value"	TEXT,
	"DataTypeId"	int NOT NULL,
	"Description"	TEXT COLLATE NOCASE,
	"IsEncrypted"	INTEGER NOT NULL,
	"IsRequired"	INTEGER NOT NULL,
	UNIQUE (ConfigurationGroupId, Name),
	CONSTRAINT "PK_ConfigurationEntry" PRIMARY KEY("Id" AUTOINCREMENT)
);

INSERT INTO ConfigurationEntry (Id, ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT Id, ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired FROM ConfigurationEntry_Upgrade;

DROP TABLE ConfigurationEntry_Upgrade;

ALTER TABLE ConfigurationGroup RENAME TO ConfigurationGroup_Upgrade;

CREATE TABLE "ConfigurationGroup" (
	"Id"	INTEGER NOT NULL,
	"Name"	TEXT NOT NULL COLLATE NOCASE,
	"Description" TEXT COLLATE NOCASE,
	UNIQUE (Name),
	CONSTRAINT "PK_ConfigurationGroup" PRIMARY KEY("Id" ASC AUTOINCREMENT)
);

INSERT INTO ConfigurationGroup (Id, Name, Description)
SELECT Id, Name, Description FROM ConfigurationGroup_Upgrade;

DROP TABLE ConfigurationGroup_Upgrade;

COMMIT TRANSACTION;
