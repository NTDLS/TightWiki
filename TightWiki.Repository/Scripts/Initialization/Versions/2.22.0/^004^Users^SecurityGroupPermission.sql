CREATE TABLE "SecurityGroupPermission" (
	"Id"	INTEGER NOT NULL UNIQUE,
	"Namespace"	TEXT COLLATE NOCASE,
	"Page"	TEXT COLLATE NOCASE,
	"SecurityGroupId"	INTEGER NOT NULL,
	"PermissionId"	INTEGER NOT NULL,
	PRIMARY KEY("Id" AUTOINCREMENT),
	FOREIGN KEY("PermissionId") REFERENCES "Permission"("Id"),
	FOREIGN KEY("SecurityGroupId") REFERENCES "Profile"("SecurityGroupId")
);