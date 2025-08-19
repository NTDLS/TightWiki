--##IF TABLE NOT EXISTS(RolePermission)

CREATE TABLE "RolePermission" (
	"Id"	INTEGER NOT NULL UNIQUE,
	"RoleId"	INTEGER NOT NULL,
	"PermissionId"	INTEGER NOT NULL,
	"Namespace"	TEXT,
	"PageId"	TEXT,
	"PermissionDispositionId"	INTEGER NOT NULL,
	PRIMARY KEY("Id" AUTOINCREMENT),
	CONSTRAINT "IX_Unique" UNIQUE("RoleId","PermissionId","Namespace","PageId","PermissionDispositionId"),
	FOREIGN KEY("PermissionDispositionId") REFERENCES "PermissionDisposition"("Id"),
	FOREIGN KEY("PermissionId") REFERENCES "Permission"("Id"),
	FOREIGN KEY("RoleId") REFERENCES "Role"("Id")
);
