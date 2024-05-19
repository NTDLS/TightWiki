SELECT
	P.Revision,
	PR.DataHash,
    P.Name,
    P.Namespace,
    P.Description
FROM
	[Page] as P
INNER JOIN [PageRevision] as PR
	ON PR.PageId = P.Id
WHERE
	P.Id = @PageId
	AND PR.Revision = COALESCE(@Revision, P.Revision)
