SELECT
	P.Id,
	P.[Name],
	P.[Description],
	P.[Revision],
	P.Navigation,
	P.CreatedByUserId,
	P.CreatedDate,
	PR.ModifiedByUserId,
	PR.ModifiedDate
FROM
	[Page] as P
INNER JOIN [PageRevision] as PR
	ON PR.PageId = P.Id
	AND PR.Revision = P.Revision
WHERE
	PR.ModifiedByUserId = @UserId
ORDER BY
	PR.ModifiedDate DESC,
	P.[Name] ASC
LIMIT @TopCount
