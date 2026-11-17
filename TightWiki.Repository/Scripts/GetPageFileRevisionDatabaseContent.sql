SELECT
	Data
FROM
	PageFileRevision
WHERE
	PageFileId = @PageFileId
	AND Revision = @Revision
