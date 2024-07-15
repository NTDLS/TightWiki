SELECT
	PR.Revision
FROM
	[PageRevision] as PR
WHERE
	PR.PageId = 1
	AND PR.Revision < 9
ORDER BY
	PR.Revision DESC
LIMIT 1;