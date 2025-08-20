SELECT
	RP.Id,
	P.Name as Permission,
	PD.Name as PermissionDisposition,
	RP.Namespace,
	RP.PageId,
	CASE
		WHEN RP.Namespace IS NOT NULL THEN
			 RP.Namespace 
		WHEN RP.PageId IS NOT NULL THEN
			CASE WHEN RP.PageId = '*' THEN '*' ELSE PG.Name END
	END as ResourceName
FROM
	RolePermission as RP
INNER JOIN Role as R
	ON R.Id = RP.RoleId
INNER JOIN Permission as P
	ON P.Id = RP.PermissionId
INNER JOIN PermissionDisposition as PD
	ON PD.Id = RP.PermissionDispositionId
LEFT OUTER JOIN pages_db.[Page] as PG
	ON Pg.Id = RP.PageId
WHERE
	R.Id = @RoleId
--CUSTOM_ORDER_BEGIN::
--CONFIG::
/*
Permission=P.Name
Disposition=PD.Name
Resource=(CASE WHEN RP.Namespace IS NOT NULL THEN 'N-' || RP.Namespace WHEN RP.PageId IS NOT NULL THEN CASE  WHEN RP.PageId = '*' THEN 'P-' || '*' ELSE 'P-' || PG.Name END END)
*/
--::CONFIG
ORDER BY
	P.Name,
	PD.Name
--::CUSTOM_ORDER_BEGIN
