--##IF TABLE NOT EXISTS(AccountPermission)

CREATE TABLE "AccountPermission" (
	"Id"	INTEGER NOT NULL UNIQUE,
	"UserId"	TEXT NOT NULL,
	"PermissionId"	INTEGER NOT NULL,
	"Namespace"	TEXT,
	"PageId"	TEXT,
	"PermissionDispositionId"	INTEGER NOT NULL,
	PRIMARY KEY("Id" AUTOINCREMENT),
	FOREIGN KEY("PermissionDispositionId") REFERENCES "PermissionDisposition"("Id"),
	FOREIGN KEY("PermissionId") REFERENCES "Permission"("Id"),
	FOREIGN KEY("UserId") REFERENCES "Profile"("UserId")
);
