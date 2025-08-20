SELECT
	AP.Id,
	P.Name as Permission,
	PD.Name as PermissionDisposition,
	AP.Namespace,
	AP.PageId,
	CASE
		WHEN AP.Namespace IS NOT NULL THEN
			 AP.Namespace 
		WHEN AP.PageId IS NOT NULL THEN
			CASE WHEN AP.PageId = '*' THEN '*' ELSE PG.Name END
	END as ResourceName,
	@PageSize as PaginationPageSize,
	(
		SELECT
			(Round(Count(0) / (@PageSize + 0.0) + 0.999))
		FROM
			AccountPermission as AP
		INNER JOIN Permission as P
			ON P.Id = AP.PermissionId
		INNER JOIN PermissionDisposition as PD
			ON PD.Id = AP.PermissionDispositionId
		LEFT OUTER JOIN pages_db.[Page] as PG
			ON Pg.Id = AP.PageId
		WHERE
			AP.UserId = @UserId
	) as PaginationPageCount
FROM
	AccountPermission as AP
INNER JOIN Permission as P
	ON P.Id = AP.PermissionId
INNER JOIN PermissionDisposition as PD
	ON PD.Id = AP.PermissionDispositionId
LEFT OUTER JOIN pages_db.[Page] as PG
	ON Pg.Id = AP.PageId
WHERE
	AP.UserId = @UserId
--CUSTOM_ORDER_BEGIN::
--CONFIG::
/*
Permission=P.Name
Disposition=PD.Name
Resource=(CASE WHEN AP.Namespace IS NOT NULL THEN 'N-' || AP.Namespace WHEN AP.PageId IS NOT NULL THEN CASE  WHEN AP.PageId = '*' THEN 'P-' || '*' ELSE 'P-' || PG.Name END END)
*/
--::CONFIG
ORDER BY
	P.Name,
	PD.Name
--::CUSTOM_ORDER_BEGIN
LIMIT @PageSize
OFFSET (@PageNumber - 1) * @PageSize
