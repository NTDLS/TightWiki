SELECT
	AP.Id,
	P.Name as Permission,
	PD.Name as PermissionDisposition,
	AP.Namespace,
	AP.PageId,
	PG.Name as PageName
FROM
	AccountPermission as AP
INNER JOIN Profile as U
	ON U.UserId = AP.UserId
INNER JOIN Permission as P
	ON P.Id = AP.PermissionId
INNER JOIN PermissionDisposition as PD
	ON PD.Id = AP.PermissionDispositionId
INNER JOIN pages_db.[Page] as PG
	ON Pg.Id = AP.PageId
WHERE
	AP.UserId = @UserId
--CUSTOM_ORDER_BEGIN::
--CONFIG::
/*
Permission=P.Name
PermissionDisposition=PD.Name
Namespace=AP.Namespace
PageName=PG.Name
*/
--::CONFIG
ORDER BY
	P.Name,
	PD.Name
--::CUSTOM_ORDER_BEGIN
