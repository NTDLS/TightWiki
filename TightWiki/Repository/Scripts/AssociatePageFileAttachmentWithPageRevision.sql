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
