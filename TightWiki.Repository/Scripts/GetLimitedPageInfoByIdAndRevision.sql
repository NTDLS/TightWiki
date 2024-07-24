SELECT
	P.Id,
    P.[Name],
    P.[Namespace],
    P.[Description],
	P.Navigation,
	PR.Revision,
	PR.DataHash,
	P.Revision as MostCurrentRevision,
	P.CreatedByUserId,
	P.CreatedDate,
	P.ModifiedByUserId,
	P.ModifiedDate
FROM
	[Page] as P
INNER JOIN [PageRevision] as PR
	ON PR.PageId = P.Id
WHERE
	P.Id = @PageId
	AND PR.Revision = COALESCE(@Revision, P.Revision)
