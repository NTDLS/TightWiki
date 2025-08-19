SELECT
	1
FROM
	RolePermission
WHERE
	RoleId = @RoleId
	AND PermissionId = @PermissionId
	AND PermissionDispositionId = @PermissionDispositionId
	AND (Namespace = @Namespace OR Namespace IS NULL)
	AND (PageId = @PageId OR PageId IS NULL)
LIMIT 1;
