SELECT
	PR.Revision
FROM
	[PageRevision] as PR
WHERE
	PR.PageId = @PageId
	AND PR.Revision < @Revision
ORDER BY
	PR.Revision DESC
LIMIT 1;