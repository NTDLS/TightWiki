--The content is now in the attachment storage. The column is NOT NULL, so it keeps an empty value.
UPDATE
	PageFileRevision
SET
	Data = X''
WHERE
	PageFileId = @PageFileId
	AND Revision = @Revision
