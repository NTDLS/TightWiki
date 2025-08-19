SELECT
	1
FROM
	AccountRole
WHERE
	UserId = @UserId
	AND RoleId = @RoleId
LIMIT 1;
