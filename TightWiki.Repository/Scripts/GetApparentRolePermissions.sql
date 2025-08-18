SELECT
	--'Role:' || R.Name as PermissionSource,
	P.Name as Permission,
	PD.Name as PermissionDisposition,
	RP.Namespace,
	RP.PageId
FROM
	RolePermission as RP
INNER JOIN Role as R
	ON R.Id = RP.RoleId
INNER JOIN Permission as P
	ON P.Id = RP.PermissionId
INNER JOIN PermissionDisposition as PD
	ON PD.Id = RP.PermissionDispositionId
WHERE
	R.[Name] = @RoleName
