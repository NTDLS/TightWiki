INSERT INTO AccountRole(UserId, RoleId)
SELECT @UserId, @RoleId;

SELECT
	AR.Id,
	R.[Name]
FROM
	AccountRole as AR
INNER JOIN Role as R
	ON R.Id = AR.RoleId
WHERE
	AR.Id = last_insert_rowid()
