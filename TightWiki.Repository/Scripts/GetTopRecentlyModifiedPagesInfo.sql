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
ORDER BY
	P.ModifiedDate DESC,
	P.[Name] ASC
LIMIT @TopCount
