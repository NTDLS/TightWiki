SELECT
	P.Id,
	P.[Name],
	P.[Description],
	P.[Revision],
	P.Navigation,
	P.CreatedByUserId,
	P.CreatedDate,
	P.ModifiedByUserId,
	P.ModifiedDate
FROM
	[Page] as P
WHERE
	P.ModifiedByUserId = @UserId
ORDER BY
	P.ModifiedDate DESC,
	P.[Name] ASC
LIMIT @TopCount