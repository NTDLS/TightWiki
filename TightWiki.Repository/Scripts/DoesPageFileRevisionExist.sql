SELECT
	COUNT(0)
FROM
	PageFileRevision
WHERE
	PageFileId = @PageFileId
	AND Revision = @Revision
