SELECT
	P.Id,
	P.[Name],
	P.[Description],
	PR.Body,
	PR.Revision,
	P.Navigation,
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
	AND PR.Revision = Coalesce(@Revision, P.Revision)
