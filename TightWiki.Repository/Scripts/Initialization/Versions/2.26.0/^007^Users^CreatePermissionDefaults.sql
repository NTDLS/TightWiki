--Migrate roles to new table:
INSERT INTO AccountRole(UserId, RoleId)
SELECT
	UPPER(UC.UserId),
	R.Id as RoleId
FROM
	AspNetUserClaims as UC
INNER JOIN Role as R
	ON R.Name = UC.ClaimValue
WHERE
	UC.ClaimType LIKE '%/role';

--Remove roles from old location
DELETE FROM AspNetUserClaims WHERE ClaimType LIKE '%/role';

--Add allow permissions for Administrator. Admin is a special role and does not really need to be assigned permissions.
INSERT INTO RolePermission(RoleId, PermissionId, Namespace, PageId, PermissionDispositionId)
SELECT
	(SELECT R.Id FROM Role as R WHERE R.Name = 'Administrator') as RoleId,
	P.Id as PermissionId,
	null as Namespace,
	'*' as PageId,
	(SELECT PD.Id FROM PermissionDisposition as PD WHERE PD.Name = 'Allow') as PermissionDispositionId
FROM
	Permission as P
UNION SELECT
	(SELECT R.Id FROM Role as R WHERE R.Name = 'Administrator') as RoleId,
	P.Id as PermissionId,
	'*' as Namespace,
	null as PageId,
	(SELECT PD.Id FROM PermissionDisposition as PD WHERE PD.Name = 'Allow') as PermissionDispositionId
FROM
	Permission as P;

--Add permissions for Anonymous.
INSERT INTO RolePermission(RoleId, PermissionId, Namespace, PageId, PermissionDispositionId)
SELECT
	(SELECT R.Id FROM Role as R WHERE R.Name = 'Anonymous') as RoleId,
	P.Id as PermissionId,
	null as Namespace,
	'*' as PageId,
	(SELECT PD.Id FROM PermissionDisposition as PD WHERE PD.Name = 'Allow') as PermissionDispositionId
FROM
	Permission as P
WHERE
	P.Name IN ('Read')
UNION SELECT
	(SELECT R.Id FROM Role as R WHERE R.Name = 'Anonymous') as RoleId,
	P.Id as PermissionId,
	'*' as Namespace,
	null as PageId,
	(SELECT PD.Id FROM PermissionDisposition as PD WHERE PD.Name = 'Allow') as PermissionDispositionId
FROM
	Permission as P
WHERE
	P.Name IN ('Read');

--Add permissions for Member.
INSERT INTO RolePermission(RoleId, PermissionId, Namespace, PageId, PermissionDispositionId)
SELECT
	(SELECT R.Id FROM Role as R WHERE R.Name = 'Member') as RoleId,
	P.Id as PermissionId,
	null as Namespace,
	'*' as PageId,
	(SELECT PD.Id FROM PermissionDisposition as PD WHERE PD.Name = 'Allow') as PermissionDispositionId
FROM
	Permission as P
WHERE
	P.Name IN ('Read')
UNION SELECT
	(SELECT R.Id FROM Role as R WHERE R.Name = 'Member') as RoleId,
	P.Id as PermissionId,
	'*' as Namespace,
	null as PageId,
	(SELECT PD.Id FROM PermissionDisposition as PD WHERE PD.Name = 'Allow') as PermissionDispositionId
FROM
	Permission as P
WHERE
	P.Name IN ('Read');

--Add permissions for Moderator.
INSERT INTO RolePermission(RoleId, PermissionId, Namespace, PageId, PermissionDispositionId)
SELECT
	(SELECT R.Id FROM Role as R WHERE R.Name = 'Moderator') as RoleId,
	P.Id as PermissionId,
	null as Namespace,
	'*' as PageId,
	(SELECT PD.Id FROM PermissionDisposition as PD WHERE PD.Name = 'Allow') as PermissionDispositionId
FROM
	Permission as P
WHERE
	P.Name IN ('Read', 'Edit', 'Delete', 'Moderate')
UNION SELECT
	(SELECT R.Id FROM Role as R WHERE R.Name = 'Moderator') as RoleId,
	P.Id as PermissionId,
	'*' as Namespace,
	null as PageId,
	(SELECT PD.Id FROM PermissionDisposition as PD WHERE PD.Name = 'Allow') as PermissionDispositionId
FROM
	Permission as P
WHERE
	P.Name IN ('Read', 'Edit', 'Delete', 'Moderate');

--Add permissions for Contributor.
INSERT INTO RolePermission(RoleId, PermissionId, Namespace, PageId, PermissionDispositionId)
SELECT
	(SELECT R.Id FROM Role as R WHERE R.Name = 'Contributor') as RoleId,
	P.Id as PermissionId,
	null as Namespace,
	'*' as PageId,
	(SELECT PD.Id FROM PermissionDisposition as PD WHERE PD.Name = 'Allow') as PermissionDispositionId
FROM
	Permission as P
WHERE
	P.Name IN ('Read', 'Edit')
UNION SELECT
	(SELECT R.Id FROM Role as R WHERE R.Name = 'Contributor') as RoleId,
	P.Id as PermissionId,
	'*' as Namespace,
	null as PageId,
	(SELECT PD.Id FROM PermissionDisposition as PD WHERE PD.Name = 'Allow') as PermissionDispositionId
FROM
	Permission as P
WHERE
	P.Name IN ('Read', 'Edit');	
