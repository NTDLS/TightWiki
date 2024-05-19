BEGIN TRANSACTION;
CREATE TABLE IF NOT EXISTS "AspNetUserTokens" (
	"UserId"	TEXT NOT NULL,
	"LoginProvider"	TEXT NOT NULL,
	"Name"	TEXT NOT NULL,
	"Value"	TEXT,
	CONSTRAINT "FK_AspNetUserTokens_AspNetUsers_UserId" FOREIGN KEY("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
	CONSTRAINT "PK_AspNetUserTokens" PRIMARY KEY("UserId","LoginProvider","Name")
);
CREATE TABLE IF NOT EXISTS "AspNetUsers" (
	"Id"	TEXT NOT NULL,
	"AccessFailedCount"	INTEGER NOT NULL,
	"ConcurrencyStamp"	TEXT,
	"Email"	TEXT,
	"EmailConfirmed"	INTEGER NOT NULL,
	"LockoutEnabled"	INTEGER NOT NULL,
	"LockoutEnd"	TEXT,
	"NormalizedEmail"	TEXT,
	"NormalizedUserName"	TEXT,
	"PasswordHash"	TEXT,
	"PhoneNumber"	TEXT,
	"PhoneNumberConfirmed"	INTEGER NOT NULL,
	"SecurityStamp"	TEXT,
	"TwoFactorEnabled"	INTEGER NOT NULL,
	"UserName"	TEXT,
	CONSTRAINT "PK_AspNetUsers" PRIMARY KEY("Id")
);
CREATE TABLE IF NOT EXISTS "AspNetUserRoles" (
	"UserId"	TEXT NOT NULL,
	"RoleId"	TEXT NOT NULL,
	CONSTRAINT "FK_AspNetUserRoles_AspNetRoles_RoleId" FOREIGN KEY("RoleId") REFERENCES "AspNetRoles"("Id") ON DELETE CASCADE,
	CONSTRAINT "FK_AspNetUserRoles_AspNetUsers_UserId" FOREIGN KEY("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
	CONSTRAINT "PK_AspNetUserRoles" PRIMARY KEY("UserId","RoleId")
);
CREATE TABLE IF NOT EXISTS "AspNetUserLogins" (
	"LoginProvider"	TEXT NOT NULL,
	"ProviderKey"	TEXT NOT NULL,
	"ProviderDisplayName"	TEXT,
	"UserId"	TEXT NOT NULL,
	CONSTRAINT "FK_AspNetUserLogins_AspNetUsers_UserId" FOREIGN KEY("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
	CONSTRAINT "PK_AspNetUserLogins" PRIMARY KEY("LoginProvider","ProviderKey")
);
CREATE TABLE IF NOT EXISTS "AspNetUserClaims" (
	"Id"	INTEGER NOT NULL,
	"ClaimType"	TEXT,
	"ClaimValue"	TEXT,
	"UserId"	TEXT NOT NULL,
	CONSTRAINT "FK_AspNetUserClaims_AspNetUsers_UserId" FOREIGN KEY("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
	CONSTRAINT "PK_AspNetUserClaims" PRIMARY KEY("Id" AUTOINCREMENT)
);
CREATE TABLE IF NOT EXISTS "AspNetRoles" (
	"Id"	TEXT NOT NULL,
	"ConcurrencyStamp"	TEXT,
	"Name"	TEXT,
	"NormalizedName"	TEXT,
	CONSTRAINT "PK_AspNetRoles" PRIMARY KEY("Id")
);
CREATE TABLE IF NOT EXISTS "AspNetRoleClaims" (
	"Id"	INTEGER NOT NULL,
	"ClaimType"	TEXT,
	"ClaimValue"	TEXT,
	"RoleId"	TEXT NOT NULL,
	CONSTRAINT "FK_AspNetRoleClaims_AspNetRoles_RoleId" FOREIGN KEY("RoleId") REFERENCES "AspNetRoles"("Id") ON DELETE CASCADE,
	CONSTRAINT "PK_AspNetRoleClaims" PRIMARY KEY("Id" AUTOINCREMENT)
);
CREATE TABLE IF NOT EXISTS "ConfigurationEntry" (
	"Id"	int IDENTITY(1, 1) NOT NULL,
	"ConfigurationGroupId"	int NOT NULL,
	"Name"	nvarchar(128) NOT NULL COLLATE NOCASE,
	"Value"	text,
	"DataTypeId"	int NOT NULL,
	"Description"	nvarchar(1000) COLLATE NOCASE,
	"IsEncrypted"	bit NOT NULL,
	CONSTRAINT "PK_ConfigurationEntry" PRIMARY KEY("ConfigurationGroupId" ASC,"Name" ASC)
);
CREATE TABLE IF NOT EXISTS "ConfigurationGroup" (
	"Id"	int IDENTITY(1, 1) NOT NULL,
	"Name"	nvarchar(128) NOT NULL COLLATE NOCASE,
	"Description"	nvarchar(1000) COLLATE NOCASE,
	CONSTRAINT "PK_ConfigurationGroup" PRIMARY KEY("Id" ASC)
);
CREATE TABLE IF NOT EXISTS "CryptoCheck" (
	"Content"	blob
);
CREATE TABLE IF NOT EXISTS "DataType" (
	"Id"	int IDENTITY(1, 1) NOT NULL,
	"Name"	nvarchar(50) COLLATE NOCASE,
	CONSTRAINT "PK_DataType" PRIMARY KEY("Id" ASC)
);
CREATE TABLE IF NOT EXISTS "Emoji" (
	"Id"	int IDENTITY(1, 1) NOT NULL,
	"Name"	nvarchar(128) NOT NULL COLLATE NOCASE,
	"ImageData"	blob,
	"MimeType"	nvarchar(50) COLLATE NOCASE,
	CONSTRAINT "IX_Emoji" UNIQUE("Name" ASC),
	CONSTRAINT "PK_Emoji" PRIMARY KEY("Id" ASC)
);
CREATE TABLE IF NOT EXISTS "EmojiCategory" (
	"Id"	int IDENTITY(1, 1) NOT NULL,
	"EmojiId"	int NOT NULL,
	"Category"	nvarchar(128) NOT NULL COLLATE NOCASE,
	CONSTRAINT "IX_EmojiCategory" UNIQUE("EmojiId" ASC,"Category" ASC),
	CONSTRAINT "PK_EmojiCategory" PRIMARY KEY("Id" ASC)
);
CREATE TABLE IF NOT EXISTS "Exception" (
	"Id"	int IDENTITY(1, 1) NOT NULL,
	"Text"	text,
	"ExceptionText"	text,
	"StackTrace"	text,
	"CreatedDate"	datetime,
	CONSTRAINT "PK_Exceptions" PRIMARY KEY("Id" ASC)
);
CREATE TABLE IF NOT EXISTS "MenuItem" (
	"Id"	int IDENTITY(1, 1) NOT NULL,
	"Name"	nvarchar(128) NOT NULL COLLATE NOCASE,
	"Link"	nvarchar(128) NOT NULL COLLATE NOCASE,
	"Ordinal"	int NOT NULL,
	CONSTRAINT "PK_MenuItem" PRIMARY KEY("Id" ASC)
);
CREATE TABLE IF NOT EXISTS "Page" (
	"Id"	int IDENTITY(1, 1) NOT NULL,
	"Name"	nvarchar(128) NOT NULL COLLATE NOCASE,
	"Namespace"	nvarchar(128) COLLATE NOCASE,
	"Navigation"	nvarchar(128) NOT NULL COLLATE NOCASE,
	"Description"	text,
	"Revision"	int NOT NULL,
	"CreatedByUserId"	int NOT NULL,
	"CreatedDate"	datetime NOT NULL,
	"ModifiedByUserId"	int NOT NULL,
	"ModifiedDate"	datetime NOT NULL,
	CONSTRAINT "PK_Page" PRIMARY KEY("Id" ASC)
);
CREATE TABLE IF NOT EXISTS "PageComment" (
	"Id"	int IDENTITY(1, 1) NOT NULL,
	"PageId"	int NOT NULL,
	"CreatedDate"	datetime NOT NULL,
	"UserId"	int NOT NULL,
	"Body"	text NOT NULL,
	CONSTRAINT "PK_PageComment" PRIMARY KEY("Id" ASC)
);
CREATE TABLE IF NOT EXISTS "PageFileRevision" (
	"PageFileId"	int NOT NULL,
	"ContentType"	nvarchar(100) NOT NULL COLLATE NOCASE,
	"Size"	int NOT NULL,
	"CreatedDate"	datetime NOT NULL,
	"Data"	blob NOT NULL,
	"Revision"	int NOT NULL,
	"DataHash"	int NOT NULL,
	CONSTRAINT "PK_PageFileRevision_1" PRIMARY KEY("PageFileId" ASC,"Revision" ASC)
);
CREATE TABLE IF NOT EXISTS "PageProcessingInstruction" (
	"PageId"	int NOT NULL,
	"Instruction"	nvarchar(128) NOT NULL COLLATE NOCASE,
	CONSTRAINT "PK_ProcessingInstruction" PRIMARY KEY("PageId" ASC,"Instruction" ASC)
);
CREATE TABLE IF NOT EXISTS "PageReference" (
	"PageId"	int NOT NULL,
	"ReferencesPageName"	nvarchar(128) NOT NULL COLLATE NOCASE,
	"ReferencesPageNavigation"	nvarchar(128) NOT NULL COLLATE NOCASE,
	"ReferencesPageId"	int,
	CONSTRAINT "PK_PageReference" PRIMARY KEY("PageId" ASC,"ReferencesPageNavigation" ASC)
);
CREATE TABLE IF NOT EXISTS "PageRevision" (
	"PageId"	int NOT NULL,
	"Name"	nvarchar(128) NOT NULL COLLATE NOCASE,
	"Namespace"	nvarchar(128) COLLATE NOCASE,
	"Description"	text NOT NULL,
	"Body"	text NOT NULL,
	"Revision"	int NOT NULL,
	"ModifiedByUserId"	int NOT NULL,
	"ModifiedDate"	datetime NOT NULL,
	"DataHash"	int NOT NULL,
	CONSTRAINT "PK_PageRevision" PRIMARY KEY("PageId" ASC,"Revision" ASC)
);
CREATE TABLE IF NOT EXISTS "PageRevisionAttachment" (
	"PageId"	int NOT NULL,
	"PageFileId"	int NOT NULL,
	"FileRevision"	int NOT NULL,
	"PageRevision"	int NOT NULL,
	CONSTRAINT "PK_PageRevisionAttachment" PRIMARY KEY("PageId" ASC,"PageFileId" ASC,"FileRevision" ASC,"PageRevision" ASC)
);
CREATE TABLE IF NOT EXISTS "PageStatistics" (
	"Id"	int IDENTITY(1, 1) NOT NULL,
	"PageId"	int NOT NULL,
	"CreatedDate"	datetime NOT NULL,
	"WikifyTimeMs"	decimal(8, 2),
	"MatchCount"	int,
	"ErrorCount"	int,
	"OutgoingLinkCount"	int,
	"TagCount"	int,
	"ProcessedBodySize"	int,
	"BodySize"	int,
	CONSTRAINT "PK_PageStatistics" PRIMARY KEY("Id" ASC)
);
CREATE TABLE IF NOT EXISTS "PageTag" (
	"PageId"	int NOT NULL,
	"Tag"	nvarchar(128) NOT NULL COLLATE NOCASE,
	CONSTRAINT "PK_PageTag" PRIMARY KEY("PageId" ASC,"Tag" ASC)
);
CREATE TABLE IF NOT EXISTS "PageToken" (
	"PageId"	int NOT NULL,
	"Token"	nvarchar(128) NOT NULL COLLATE NOCASE,
	"Weight"	decimal(6, 2) NOT NULL,
	"DoubleMetaphone"	varchar(16) NOT NULL,
	CONSTRAINT "PK_PageToken" PRIMARY KEY("PageId" ASC,"Token" ASC)
);
CREATE TABLE IF NOT EXISTS "Role" (
	"Id"	int IDENTITY(1, 1) NOT NULL,
	"Name"	nvarchar(128) NOT NULL COLLATE NOCASE,
	"Description"	nvarchar(1000) COLLATE NOCASE,
	CONSTRAINT "PK_Role" PRIMARY KEY("Id" ASC)
);
CREATE TABLE IF NOT EXISTS "User" (
	"Id"	int IDENTITY(1, 1) NOT NULL,
	"EmailAddress"	nvarchar(128) NOT NULL COLLATE NOCASE,
	"AccountName"	nvarchar(128) NOT NULL COLLATE NOCASE,
	"Navigation"	nvarchar(128) COLLATE NOCASE,
	"PasswordHash"	nvarchar(128) COLLATE NOCASE,
	"FirstName"	nvarchar(128) COLLATE NOCASE,
	"LastName"	nvarchar(128) COLLATE NOCASE,
	"Country"	nvarchar(128) NOT NULL COLLATE NOCASE,
	"Language"	nvarchar(128) NOT NULL COLLATE NOCASE,
	"TimeZone"	nvarchar(128) NOT NULL COLLATE NOCASE,
	"AboutMe"	text,
	"Avatar"	blob,
	"CreatedDate"	datetime NOT NULL,
	"ModifiedDate"	datetime NOT NULL,
	"LastLoginDate"	datetime NOT NULL,
	"VerificationCode"	varchar(20),
	"EmailVerified"	bit NOT NULL,
	"RoleId"	int NOT NULL,
	"Provider"	varchar(20) NOT NULL,
	"Deleted"	bit NOT NULL,
	CONSTRAINT "PK_User" PRIMARY KEY("Id" ASC)
);
CREATE TABLE IF NOT EXISTS "PageFile" (
	"Id"	int IDENTITY(1, 1) NOT NULL,
	"PageId"	int NOT NULL,
	"Name"	nvarchar(250) NOT NULL COLLATE NOCASE,
	"Navigation"	nvarchar(250) NOT NULL COLLATE NOCASE,
	"Revision"	int NOT NULL,
	"CreatedDate"	datetime NOT NULL,
	CONSTRAINT "PK_PageFile" PRIMARY KEY("Id" ASC)
);
CREATE INDEX IF NOT EXISTS "EmailIndex" ON "AspNetUsers" (
	"NormalizedEmail"
);
CREATE UNIQUE INDEX IF NOT EXISTS "UserNameIndex" ON "AspNetUsers" (
	"NormalizedUserName"
);
CREATE INDEX IF NOT EXISTS "IX_AspNetUserRoles_RoleId" ON "AspNetUserRoles" (
	"RoleId"
);
CREATE INDEX IF NOT EXISTS "IX_AspNetUserLogins_UserId" ON "AspNetUserLogins" (
	"UserId"
);
CREATE INDEX IF NOT EXISTS "IX_AspNetUserClaims_UserId" ON "AspNetUserClaims" (
	"UserId"
);
CREATE UNIQUE INDEX IF NOT EXISTS "RoleNameIndex" ON "AspNetRoles" (
	"NormalizedName"
);
CREATE INDEX IF NOT EXISTS "IX_AspNetRoleClaims_RoleId" ON "AspNetRoleClaims" (
	"RoleId"
);
COMMIT;
