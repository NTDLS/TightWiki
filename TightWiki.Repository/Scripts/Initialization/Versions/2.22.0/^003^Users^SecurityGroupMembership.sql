CREATE TABLE "SecurityGroupMembership" (
	"UserId"	INTEGER NOT NULL,
	"SecurityGroupId"	INTEGER NOT NULL,
	FOREIGN KEY("SecurityGroupId") REFERENCES "SecurityGroup"("Id"),
	FOREIGN KEY("UserId") REFERENCES "Profile"("UserId")
);