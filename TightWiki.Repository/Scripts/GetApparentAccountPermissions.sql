SELECT
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
INNER JOIN AccountRole as AR
	ON AR.RoleId =  RP.RoleId
WHERE
	AR.UserId = @UserId

UNION

SELECT
	P.Name as Permission,
	PD.Name as PermissionDisposition,
	AP.Namespace,
	AP.PageId
FROM
	AccountPermission as AP
INNER JOIN Profile as U
	ON U.UserId = AP.UserId
INNER JOIN Permission as P
	ON P.Id = AP.PermissionId
INNER JOIN PermissionDisposition as PD
	ON PD.Id = AP.PermissionDispositionId
WHERE
	AP.UserId = @UserId
