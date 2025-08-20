SELECT
	1
FROM
	Profile as P
INNER JOIN AccountRole as AR
	ON AR.UserId = P.UserId
INNER JOIN Role as R
	ON R.Id = AR.RoleId
WHERE
	P.UserId = @UserId
	AND R.Name = 'Administrator'
LIMIT 1
