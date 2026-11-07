SELECT
	Id,
	Name,
	CreatedDate
FROM
	ApiTokens
WHERE
	UserId = @UserId
ORDER BY
	CreatedDate DESC
