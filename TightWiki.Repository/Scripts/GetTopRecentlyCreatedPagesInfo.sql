SELECT
	P.Id,
	P.[Name],
	P.[Description],
	P.[Revision],
	P.Navigation,
	P.CreatedByUserId,
	P.CreatedDate,
	P.ModifiedByUserId,
	IFNULL(P.ModifiedDate, P.CreatedDate) as ModifiedDate
FROM
	[Page] as P
ORDER BY
	P.CreatedDate DESC,
	P.[Name] ASC
LIMIT @TopCount
