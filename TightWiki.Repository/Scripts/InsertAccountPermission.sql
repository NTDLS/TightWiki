INSERT INTO AccountPermission(UserId, PermissionId, [Namespace], PageId, PermissionDispositionId)
SELECT @UserId, @PermissionId, @Namespace, @PageId, @PermissionDispositionId;

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
	END as ResourceName
FROM
	AccountPermission as AP
INNER JOIN Permission as P
	ON P.Id = AP.PermissionId
INNER JOIN PermissionDisposition as PD
	ON PD.Id = AP.PermissionDispositionId
LEFT OUTER JOIN pages_db.[Page] as PG
	ON Pg.Id = AP.PageId
WHERE
	AP.Id = last_insert_rowid()
