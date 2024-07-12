--Remove the previous page file revision attachment, if any.
DELETE FROM PageRevisionAttachment
WHERE
	PageId = @PageId
	AND PageFileId = @PageFileId
	AND FileRevision = @PreviousFileRevision
	AND PageRevision = @PageRevision;

--Associate the file revision record with the page revision.
INSERT INTO PageRevisionAttachment
(
	PageId,
	PageFileId,
	FileRevision,
	PageRevision
)
SELECT
	@PageId,
	@PageFileId,
	@FileRevision,
	@PageRevision;
