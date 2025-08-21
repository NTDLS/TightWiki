--Add CREATE on ALL namespaces permissions for Moderator.
INSERT INTO RolePermission(RoleId, PermissionId, Namespace, PageId, PermissionDispositionId)
SELECT
	(SELECT R.Id FROM Role as R WHERE R.Name = 'Moderator') as RoleId,
	P.Id as PermissionId,
	'*' as Namespace,
	null as PageId,
	(SELECT PD.Id FROM PermissionDisposition as PD WHERE PD.Name = 'Allow') as PermissionDispositionId
FROM
	Permission as P
WHERE
	P.Name IN ('Create')
	AND NOT EXISTS (
		SELECT
			1
		FROM
			RolePermission as RP
		INNER JOIN Role as R
			ON R.Id = RP.RoleId
		INNER JOIN Permission as P
			ON P.Id = RP.PermissionId
		WHERE
			R.Name = 'Moderator'
			AND P.Name = 'Create'
	);
